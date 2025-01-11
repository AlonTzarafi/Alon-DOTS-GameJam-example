using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public partial struct ProjectileSystem : ISystem
{
    private EntityQuery query;

    public void OnCreate(ref SystemState state)
    {
        query = state.EntityManager.CreateEntityQuery(
            typeof(Targetable),
            typeof(LocalTransform),
            typeof(Quadrant)
        );
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        // Get all entities with a Dancer component.
        var targetables = query.ToComponentDataArray<Targetable>(Allocator.TempJob);
        var targetableTransforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var targetableQuadrants = query.ToComponentDataArray<Quadrant>(Allocator.TempJob);
        var targetableEntities = query.ToEntityArray(Allocator.TempJob);

        var job = new ProjectileUpdateJob
        {
            Targetables = targetables,
            TargetableTransforms = targetableTransforms,
            TargetableQuadrants = targetableQuadrants,
            TargetableEntities = targetableEntities,
            DeltaTime = SystemAPI.Time.DeltaTime,
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb
        };

        var jobHandle = job.ScheduleParallel(state.Dependency);

        // Dispose after the job has completed
        targetables.Dispose(jobHandle);
        targetableTransforms.Dispose(jobHandle);
        targetableQuadrants.Dispose(jobHandle);
        targetableEntities.Dispose(jobHandle);

        // Update the system dependency to include this job
        state.Dependency = jobHandle;
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}

[BurstCompile]
public partial struct ProjectileUpdateJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public float DeltaTime;
    public double ElapsedTime;

    [ReadOnly] public NativeArray<Targetable> Targetables;
    [ReadOnly] public NativeArray<LocalTransform> TargetableTransforms;
    [ReadOnly] public NativeArray<Quadrant> TargetableQuadrants;
    [ReadOnly] public NativeArray<Entity> TargetableEntities;

    private void Execute(
        [ChunkIndexInQuery] int chunkIndex,
        Entity entity,
        RefRO<Projectile> projectile,
        RefRO<MovementDirection> movementDirection,
        RefRW<LocalTransform> localTransform,
        [ReadOnly] in Quadrant projectileQuadrant
    )
    {
        var speed = projectile.ValueRO.MovementSpeed;
        var direction = movementDirection.ValueRO.Value;
        var position = localTransform.ValueRW.Position;

        var newPosition = position + direction * speed * DeltaTime;
        localTransform.ValueRW = LocalTransform.FromPosition(newPosition);


        var projectilePosition = newPosition;
        var hit = Entity.Null;
        var hitRadius = projectile.ValueRO.CollisionRadius;
        var hitCollisionId = projectile.ValueRO.CollisionId;
        var hitTargetPosition = float3.zero;
        for (int i = 0; i < Targetables.Length; i++)
        {
            if (TargetableQuadrants[i].Index != projectileQuadrant.Index) {
                continue;
            }

            var targetable = Targetables[i];
            var targetableTransform = TargetableTransforms[i];
            var targetableEntity = TargetableEntities[i];

            var targetCollisionId = targetable.CollisionId;
            var match = hitCollisionId == targetCollisionId;
            if (!match) {
                continue;
            }

            var targetPosition = targetableTransform.Position;
            var targetRadius = targetable.CollisionRadius;

            var toTarget = targetPosition - projectilePosition;
            var distanceSq = math.lengthsq(toTarget);
            
            var distanceNeededSq = (hitRadius + targetRadius) * (hitRadius + targetRadius);
            if (distanceSq < distanceNeededSq) {
                // Hit!
                hit = targetableEntity;
                hitTargetPosition = targetPosition;
                break;
            }
        }

        if (hit != Entity.Null) {
            // Hit!
            var prefabToInstantiate = projectile.ValueRO.PrefabOnHit;
            var prefabToInstantiateRandomRotation = projectile.ValueRO.PrefabOnHitRandomRotation;

            var prefabToInstantiatePosition = localTransform.ValueRO.Position;
            // Lerp to the target a bit. Pretend that we were actually closer to it when we hit it
            var lerpT = 0.6f;
            prefabToInstantiatePosition = math.lerp(prefabToInstantiatePosition, hitTargetPosition, lerpT);
            // prefabToInstantiatePosition = hitTargetPosition;

            Ecb.AddComponent(chunkIndex, entity, new ToBeDestroyed());
            Ecb.AddComponent(chunkIndex, hit, new ToBeDestroyed());

            if (prefabToInstantiate != Entity.Null) {
                var newEntity = Ecb.Instantiate(chunkIndex, prefabToInstantiate);

                var prefabToInstantiateRotation = quaternion.identity;

                if (prefabToInstantiateRandomRotation) {
                    var random = new Random((uint)(hitTargetPosition.x * 1000000.0 + hitTargetPosition.y * 253882.0 + hitTargetPosition.z * 483 + ElapsedTime * 12400.0));
                    var rotX = random.NextFloat(-math.PI, math.PI);
                    var rotY = random.NextFloat(-math.PI, math.PI);
                    var rotZ = random.NextFloat(-math.PI, math.PI);
                    prefabToInstantiateRotation = quaternion.Euler(rotX, rotY, rotZ);
                }

                Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPositionRotation(prefabToInstantiatePosition, prefabToInstantiateRotation));
            }
        }

        var minZ = projectile.ValueRO.MinZ;
        var maxZ = projectile.ValueRO.MaxZ;
        if (position.z < minZ || position.z > maxZ) {
            Ecb.AddComponent(chunkIndex, entity, new ToBeDestroyed());
        }
    }
}
