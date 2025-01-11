using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(DancerSystem))]
[BurstCompile]
public partial struct BoosterSystem : ISystem
{
    private EntityQuery query;

    public void OnCreate(ref SystemState state)
    {
        query = state.EntityManager.CreateEntityQuery(typeof(Targetable), typeof(EnemyShip), typeof(LocalTransform));
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        // Get all entities with a Dancer component.
        // var dancers = query.ToComponentDataArray<Turret>(Allocator.TempJob);
        var targetables = query.ToComponentDataArray<Targetable>(Allocator.TempJob);
        var targetableEnemyShips = query.ToComponentDataArray<EnemyShip>(Allocator.TempJob);
        var targetableTransforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var targetableEntities = query.ToEntityArray(Allocator.TempJob);

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        var job = new BoosterUpdateJob
        {
            Targetables = targetables,
            TargetableEnemyShips = targetableEnemyShips,
            TargetableTransforms = targetableTransforms,
            TargetableEntities = targetableEntities,
            DeltaTime = SystemAPI.Time.DeltaTime,
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb
        };

        // Schedule the job and retrieve the JobHandle
        var jobHandle = job.ScheduleParallel(state.Dependency);

        // Schedule disposal of the NativeArrays once the job completes
        targetables.Dispose(jobHandle);
        targetableEnemyShips.Dispose(jobHandle);
        targetableTransforms.Dispose(jobHandle);
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
public partial struct BoosterUpdateJob: IJobEntity
{
    [ReadOnly] public NativeArray<Targetable> Targetables;
    [ReadOnly] public NativeArray<EnemyShip> TargetableEnemyShips;
    [ReadOnly] public NativeArray<LocalTransform> TargetableTransforms;
    [ReadOnly] public NativeArray<Entity> TargetableEntities;
    
    public float DeltaTime;
    public double ElapsedTime;

    public EntityCommandBuffer.ParallelWriter Ecb;

    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, RefRW<Dancer> dancer, RefRW<Booster> booster, RefRO<CanSpawn> canSpawn, RefRW<LocalTransform> localTransform)
    {
        float3 movementToTarget;

        var pos = localTransform.ValueRW.Position;
        {
            var speedToSquadronOriginalCenter = booster.ValueRO.SpeedToSquadronOriginalCenter;
            var squadronCenter = booster.ValueRO.SquadronOriginalCenter;
            var toSquadronCenter = squadronCenter - pos;
            var norm = math.normalizesafe(toSquadronCenter);
            var distToMove = speedToSquadronOriginalCenter * DeltaTime;
            distToMove = math.min(distToMove, math.length(toSquadronCenter));
            var movement = norm * distToMove;
            pos = pos + movement;
        }

        {
            var speed = booster.ValueRO.Speed;
            var nextPos = booster.ValueRO.Target;
            var toNextPos = nextPos - pos;
            var norm = math.normalizesafe(toNextPos);
            var distToMove = speed * DeltaTime;
            distToMove = math.min(distToMove, math.length(toNextPos));
            var movement = norm * distToMove;
            movementToTarget = movement;
            pos = pos + movement;
        }
        var finalPos = pos;
        // localTransform.ValueRW.Position = finalPos;

        // Rotate towards target
        var targetRotation = quaternion.LookRotation(movementToTarget, math.up());
        var rotationLerpSpeed = 1.5f;
        var t = math.saturate(DeltaTime * rotationLerpSpeed);
        var finalRotation = math.slerp(localTransform.ValueRW.Rotation, targetRotation, t);
        // localTransform.ValueRW.Rotation = finalRotation;

        Ecb.SetComponent(chunkIndex, entity, new LocalTransform
        {
            Position = finalPos,
            Rotation = finalRotation,
            Scale = localTransform.ValueRO.Scale
        });

        booster.ValueRW.TimeElapsed += DeltaTime;

        booster.ValueRW.TimeUntilFart -= DeltaTime;
        if (booster.ValueRW.TimeUntilFart <= 0.0f)
        {
            booster.ValueRW.TimeUntilFart = Booster.FART_RATE;

            var fartPrefab = canSpawn.ValueRO.Prefab;
            if (booster.ValueRW.TimeElapsed > Booster.FART_TIME_LIMIT)
            {
                fartPrefab = canSpawn.ValueRO.Prefab2;
            }
            var newEntity = Ecb.Instantiate(chunkIndex, fartPrefab);
            Ecb.SetComponent(chunkIndex, newEntity, new LocalTransform
            {
                Position = finalPos,
                Rotation = finalRotation,
                Scale = 1.0f
            });
            // Parent it to the booster entity
            Ecb.AddBuffer<Child>(chunkIndex, entity).Add(new Child { Value = newEntity });
        }


        // Find closest Enemy Targetable
        var minMaxZ = new float2(-9999f, 9999f);
        var closestEnemyEntityIndex = Helpers.FindClosestEntityIndex(finalPos, minMaxZ, TargetableTransforms);
        // If found, Fly even faster towards it. And if close enough, EXPLODE ON IT, kamikaze style
        // var minDistanceToChase = 15.0f;
        // // var minDistanceToChaseForCloserToCameraEnemies = 24.0f;
        // var minDistanceToChaseForCloserToCameraEnemies = 15.0f;

        var myZ = finalPos.z;
        var closeZ = -10.0f;
        var farZ = 88.0f;
        var closeDistanceToChase = 20.0f;
        var farDistanceToChase = 4.0f;
        var closeToFarZ = math.unlerp(closeZ, farZ, myZ);
        var minDistanceToChase = math.lerp(closeDistanceToChase, farDistanceToChase, math.pow(closeToFarZ, 0.25f) * closeToFarZ * 3.3333f);
        minDistanceToChase = math.clamp(minDistanceToChase, farDistanceToChase, closeDistanceToChase);
        
        var minDistanceToExplode = 1.4f;
        if (closestEnemyEntityIndex != -1) {
            var closestEnemyEntity = TargetableEntities[closestEnemyEntityIndex];
            var closestEnemyPosition = TargetableTransforms[closestEnemyEntityIndex].Position;
            var distanceToClosestEnemyXY = math.distance(finalPos.xy, closestEnemyPosition.xy);
            var distanceToClosestEnemyZ = math.abs(finalPos.z - closestEnemyPosition.z);
            var closeEnoughToExplode = distanceToClosestEnemyXY < minDistanceToExplode;
            var closeEnoughToChase =
                distanceToClosestEnemyXY < minDistanceToChase
                &&
                distanceToClosestEnemyZ < minDistanceToChase * 6.0f;

            // Old code that made it easier to target close-to-camera enemies. Now works differently
            // var enemyIsAnnoyinglyCloseToCamera = closestEnemyPosition.z < 88.0f;
            // if (enemyIsAnnoyinglyCloseToCamera && finalPos.z > closestEnemyPosition.z) {
            //     if (distanceToClosestEnemy < minDistanceToChaseForCloserToCameraEnemies) {
            //         closeEnoughToChase = true;
            //     }
            // }
            
            if (closeEnoughToExplode) {
                // Explode on it
                Ecb.AddComponent(chunkIndex, entity, new ToBeDestroyed());
                // Ecb.AddComponent(chunkIndex, closestEnemyEntity, new ToBeDestroyed());
                Ecb.AppendToBuffer<DamageInstance>(chunkIndex, closestEnemyEntity, new DamageInstance { Damage = 1 });
                // Spawn the AnotherPrefab on it
                var explosionPrefab = canSpawn.ValueRO.AnotherPrefab;
                var explosionPosition = closestEnemyPosition;
                var random = new Random((uint)(closestEnemyPosition.x * 1000000.0 + closestEnemyPosition.y * 253882.0 + closestEnemyPosition.z * 483 + ElapsedTime * 12400.0));
                var randomRangeAdd = 1.0f;
                explosionPosition += random.NextFloat3(-randomRangeAdd, randomRangeAdd);
                var explosionRotation = quaternion.Euler(random.NextFloat3(-180f, 180f));
                
                var newEntity = Ecb.Instantiate(chunkIndex, explosionPrefab);
                Ecb.SetComponent(chunkIndex, newEntity, new LocalTransform
                {
                    Position = explosionPosition,
                    Rotation = explosionRotation,
                    Scale = random.NextFloat(0.5f, 1.5f)
                });
                // Also try to push the hit enemy a bit backwards
                var enemyHitPosition = closestEnemyPosition;
                var enemyHitRotation = TargetableTransforms[closestEnemyEntityIndex].Rotation;
                var enemyHitScale = TargetableTransforms[closestEnemyEntityIndex].Scale;
                enemyHitPosition.z += 0.025f;
                Ecb.SetComponent(chunkIndex, closestEnemyEntity, new LocalTransform
                {
                    Position = enemyHitPosition,
                    Rotation = enemyHitRotation,
                    Scale = enemyHitScale
                });
                
            } else if (closeEnoughToChase) {
                // Disable normal flying, to ensure we only fly towards the target and that's it
                dancer.ValueRW.Speed = 0.001f;
                booster.ValueRW.Speed = 0.001f;
                booster.ValueRW.SpeedToSquadronOriginalCenter = 0.001f;

                // Fly faster towards it
                var speed = MouseRaycastSystem.BOOSTER_CHASE_SPEED;
                var toClosestEnemy = closestEnemyPosition - finalPos;
                var norm = math.normalizesafe(toClosestEnemy);
                var distToMove = speed * DeltaTime;
                distToMove = math.min(distToMove, math.length(toClosestEnemy));
                var movement = norm * distToMove;
                finalPos = finalPos + movement;
                Ecb.SetComponent(chunkIndex, entity, new LocalTransform
                {
                    Position = finalPos,
                    Rotation = finalRotation,
                    Scale = localTransform.ValueRO.Scale
                });

                // Ensure as it boosts towards enemy it keeps farting some primary farts out there
                booster.ValueRW.TimeElapsed = 0.0f;
            }
        }
    }
}
