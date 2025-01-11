using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public partial struct ExplosionSystem : ISystem
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

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        var job = new ExplosionUpdateJob
        {
            Targetables = targetables,
            TargetableTransforms = targetableTransforms,
            TargetableQuadrants = targetableQuadrants,
            TargetableEntities = targetableEntities,
            DeltaTime = SystemAPI.Time.DeltaTime,
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb
        };

        // Schedule the job and retrieve the JobHandle
        var jobHandle = job.ScheduleParallel(state.Dependency);

        // Schedule disposal of the NativeArrays once the job completes
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
public partial struct ExplosionUpdateJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public float DeltaTime;
    public double ElapsedTime;

    [ReadOnly] public NativeArray<Targetable> Targetables;
    [ReadOnly] public NativeArray<LocalTransform> TargetableTransforms;
    [ReadOnly] public NativeArray<Quadrant> TargetableQuadrants;
    [ReadOnly] public NativeArray<Entity> TargetableEntities;

    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, RefRW<Explosion> explosion, RefRO<Quadrant> quadrant, RefRW<LocalTransform> localTransform)
    {
        explosion.ValueRW.TimeToLive -= DeltaTime;

        var explosionPosition = localTransform.ValueRW.Position;
        var explosionScale = localTransform.ValueRW.Scale;

        var shrinkSpeed = explosion.ValueRO.ShrinkSpeed;
        var expand = false;
        if (shrinkSpeed < 0f) {
            shrinkSpeed = -shrinkSpeed;
            expand = true;
        }
        var finalScale = 0f;
        if (expand) {
            finalScale = 1000f;
        }
        var t = shrinkSpeed * DeltaTime;
        var newScale = math.lerp(explosionScale, finalScale, t);
        localTransform.ValueRW.Position = explosionPosition;
        localTransform.ValueRW.Scale = newScale;

        // Collision detection
        var canDoDamage = Quadrant.IsInitialized(quadrant.ValueRO.UpdateMode);
        var canCollide = Helpers.CanCollideWithAnything(explosion.ValueRO.KillCollisionId);
        var hitRadius = explosion.ValueRO.KillCollisionRadius;
        var explodable = canDoDamage && canCollide && hitRadius > 0f;

        var isFirstTime = explosion.ValueRO.DealtDamageCount == 0;

        var explodeNow = explodable && isFirstTime;
        if (explodeNow) {
            explosion.ValueRW.DealtDamageCount = 1;
            var hitCollisionId = explosion.ValueRO.KillCollisionId;
            for (int i = 0; i < Targetables.Length; i++) {

                if (explosion.ValueRO.KillLimitToQuadrant) {
                    if (TargetableQuadrants[i].Index != quadrant.ValueRO.Index) {
                        continue;
                    }
                } else {
                    // Not limited. Allow explosions to damage all quadrants.
                    // This is important for bigger explosions because they have huge blast radius and can blast things in other quadrants...
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

                var toTarget = targetPosition - explosionPosition;
                var distanceSq = math.lengthsq(toTarget);

                var distanceNeeded = hitRadius + targetRadius;
                var distanceNeededSq = distanceNeeded * distanceNeeded;
                if (distanceSq < distanceNeededSq) {
                    // Hit!
                    Ecb.AddComponent(chunkIndex, targetableEntity, new ToBeDestroyed());
                }
            }
        }

        // Don't destroy explosion until it had the chance to explode
        var alreadyDealtExplosiveDamage = explosion.ValueRO.DealtDamageCount > 0;
        var preventDestructionWhenWaitingForExplosion = explodable && !alreadyDealtExplosiveDamage;
        if (explosion.ValueRO.TimeToLive <= 0f && !preventDestructionWhenWaitingForExplosion) {
            Ecb.AddComponent(chunkIndex, entity, new ToBeDestroyed());
        }
    }
}
