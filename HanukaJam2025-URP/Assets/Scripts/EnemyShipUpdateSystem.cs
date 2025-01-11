using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public partial struct EnemyUpdateSystem : ISystem
{
    private EntityQuery query;

    public void OnCreate(ref SystemState state)
    {
        // all Dancers
        query = state.EntityManager.CreateEntityQuery(typeof(Dancer), typeof(LocalTransform));
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        // Get all entities with a Dancer component.
        // var dancers = query.ToComponentDataArray<Turret>(Allocator.TempJob);
        var dancerTransforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var dancerEntities = query.ToEntityArray(Allocator.TempJob);

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        var job = new EnemyUpdateUpdateJob
        {
            DancerTransforms = dancerTransforms,
            DancerEntities = dancerEntities,
            DeltaTime = SystemAPI.Time.DeltaTime,
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb
        };

        // Schedule the job and retrieve the JobHandle
        var jobHandle = job.ScheduleParallel(state.Dependency);

        // Schedule disposal of the NativeArrays once the job completes
        dancerTransforms.Dispose(jobHandle);
        dancerEntities.Dispose(jobHandle);

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
public partial struct EnemyUpdateUpdateJob: IJobEntity
{
    public float DeltaTime;
    public double ElapsedTime;

    [ReadOnly] public NativeArray<LocalTransform> DancerTransforms;
    [ReadOnly] public NativeArray<Entity> DancerEntities;

    public EntityCommandBuffer.ParallelWriter Ecb;

    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, RefRW<EnemyShip> enemyShip, RefRO<EnemyShipConfig> enemyShipConfig, RefRW<LocalTransform> localTransform, RefRW<EnemyStatus> enemyStatus, DynamicBuffer<DamageInstance> damageInstances)
    {
        // float moveSpeedPassive = 0;
        float moveSpeedTowardsTarget = 2.2f;

        var isDying = enemyShip.ValueRW.TimeSpentAtZeroHealth > 0f;

        // Basic movement
        // localTransform.ValueRW.Position.z += -moveSpeedPassive * DeltaTime;

        var z = localTransform.ValueRW.Position.z;

        var spinSpeed = 1.0f;
        if (enemyShipConfig.ValueRO.CrazyLaserOrb) {
            spinSpeed = 0.0f;
        }
        if (isDying) {
            spinSpeed = 0.0f;
        }
        enemyShip.ValueRW.Spin += spinSpeed * DeltaTime;

        enemyShip.ValueRW.TargetPosition = localTransform.ValueRW.Position + new float3(0f, 0f, -1000f);
        
        var hasTarget = enemyShip.ValueRW.Target != Entity.Null;
        if (!hasTarget) {
            var allowedToSeekTarget = z < 70f;
            if (allowedToSeekTarget) {
                enemyShip.ValueRW.SeekRandomTargetTimer -= DeltaTime;
                if (enemyShip.ValueRW.SeekRandomTargetTimer <= 0f) {
                    enemyShip.ValueRW.SeekRandomTargetTimer = enemyShip.ValueRW.SeekRandomTargetDelay;
                    var desiredIndex = enemyShip.ValueRW.SeekRandomTargetIndex % DancerEntities.Length;
                    if (DancerEntities.Length > 0) {
                        enemyShip.ValueRW.Target = DancerEntities[desiredIndex];
                    }
                }
            }
        }

        if (hasTarget) {
            var targetIndex = -1;
            for (var i = 0; i < DancerEntities.Length; i++) {
                if (DancerEntities[i] == enemyShip.ValueRW.Target) {
                    targetIndex = i;
                    break;
                }
            }
            if (targetIndex == -1) {
                enemyShip.ValueRW.Target = Entity.Null;
            } else {
                enemyShip.ValueRW.TargetPosition = DancerTransforms[targetIndex].Position;
            }
        }


        // extra movement
        var targetDirection = enemyShip.ValueRW.TargetPosition - localTransform.ValueRW.Position;
        var move = math.normalize(targetDirection) * moveSpeedTowardsTarget * DeltaTime;
        if (isDying) {
            move = 0f;
        }
        localTransform.ValueRW.Position += move;

        // Rotate to face the target, but with extra spin around the forward direction
        if (!isDying) {
            // First seek the forward direction to target
            {
                var targetRotation = quaternion.LookRotationSafe(targetDirection, math.up());
                localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRW.Rotation, targetRotation, 1.0f * DeltaTime);
            }

            // Then add the spin
            {
                var spin = quaternion.RotateZ(enemyShip.ValueRW.Spin);
                localTransform.ValueRW.Rotation = math.mul(localTransform.ValueRW.Rotation, spin);
            }
        }


        var inPositionToShootLasers = z < 80f;
        
        if (isDying) {
            // Disable laser shooting
            inPositionToShootLasers = false;
        }

        if (enemyShipConfig.ValueRO.CrazyLaserOrb && inPositionToShootLasers) {
            // Handle shooting laz0rs
            enemyShip.ValueRW.LaserCycle += DeltaTime;
            enemyShip.ValueRW.LaserCooldown -= DeltaTime;
            var laserCycleInt = (int)enemyShip.ValueRW.LaserCycle;
            var canShoot = (laserCycleInt % 8) < 5;
            if (canShoot && enemyShip.ValueRW.LaserCooldown <= 0f) {
                enemyShip.ValueRW.LaserCooldown += 0.02f;
                var laserShooterPosition = localTransform.ValueRW.Position;
                var minMaxZ = new float2(-9999f, 9999f);
                // var laserZRange = 20f;
                // var minMaxZ = new float2(laserShooterPosition.z - laserZRange, laserShooterPosition.z + laserZRange);
                var closestShipIndex = Helpers.FindClosestEntityIndex(laserShooterPosition, minMaxZ, DancerTransforms);
                
                if (closestShipIndex != -1) {
                    var closestShip = DancerEntities[closestShipIndex];
                    var closestShipPosition = DancerTransforms[closestShipIndex].Position;
                    
                    // Remove it by adding a ToBeDestroyed component
                    Ecb.AddComponent(chunkIndex, closestShip, new ToBeDestroyed());

                    // Add a laz0r beam effect
                    var laserPrefab = enemyShipConfig.ValueRO.CrazyLaserOrbLaserPrefab;
                    if (laserPrefab != Entity.Null) {
                        var laserPosition = laserShooterPosition;
                        var laserDirection = math.normalize(closestShipPosition - laserShooterPosition);
                        var laserLength = math.distance(closestShipPosition, laserShooterPosition);
                        var laserScale = new float3(1f, 1f, laserLength);

                        var newEntity = Ecb.Instantiate(chunkIndex, laserPrefab);

                        Ecb.SetComponent(chunkIndex, newEntity, new LocalTransform
                        {
                            Position = laserPosition,
                            Rotation = quaternion.LookRotationSafe(laserDirection, math.up()),
                            Scale = 1f  // set to 1 here if you intend to do non-uniform scaling via PostTransformMatrix
                        });

                        float4x4 postTransform = float4x4.Scale(new float3(1f, 1f, laserLength));
                        Ecb.AddComponent(chunkIndex, newEntity, new PostTransformMatrix
                        {
                            Value = postTransform
                        });

                        // Also Laser Explosion
                        var laserExplosionPrefab = enemyShipConfig.ValueRO.CrazyLaserOrbLaserExplosionPrefab;
                        if (laserExplosionPrefab != Entity.Null) {
                            var explosionPosition = closestShipPosition;
                            var newExplosionEntity = Ecb.Instantiate(chunkIndex, laserExplosionPrefab);
                            Ecb.SetComponent(chunkIndex, newExplosionEntity, LocalTransform.FromPosition(explosionPosition));
                        }
                    }

                }
            }
        }

        var totalDamage = 0;
        for (var i = 0; i < damageInstances.Length; i++) {
            totalDamage += damageInstances[i].Damage;
        }

        var maxHealth = enemyStatus.ValueRW.TotalHealth;
        
        var zeroHealth = totalDamage >= maxHealth;
        var explosionDeathNow = false;
        if (zeroHealth) {
            var firstMomentOfZeroHealth = enemyShip.ValueRW.TimeSpentAtZeroHealth == 0f;
            if (firstMomentOfZeroHealth) {
                // Instantiate the death delay prefab
                var deathDelayPrefab = enemyShipConfig.ValueRO.DeathDelayPrefab;
                if (deathDelayPrefab != Entity.Null) {
                    var deathDelayPosition = localTransform.ValueRW.Position;
                    var newEntity = Ecb.Instantiate(chunkIndex, deathDelayPrefab);
                    Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(deathDelayPosition));
                }
            }
            
            enemyShip.ValueRW.TimeSpentAtZeroHealth += DeltaTime;
            if (enemyShip.ValueRW.TimeSpentAtZeroHealth > enemyShipConfig.ValueRO.DeathDelay) {
                explosionDeathNow = true;
            }

            enemyStatus.ValueRW.IsTooDamagedToFunction = true;
        }

        // var silentDeath = z < -10f;
        var silentDeath = z < -0f;
        var die = silentDeath || explosionDeathNow;
        if (silentDeath) {
            // You know what? When you reach the end (towards the camera) just make it boom and hurt the player
            // It is the player's fault anyway for letting it get this far
            explosionDeathNow = true;
        }
        if (die) {
            Ecb.AddComponent(chunkIndex, entity, new ToBeDestroyed());

            if (explosionDeathNow) {
                // Instantiate the explosion
                {
                    var explosionPrefab = enemyShipConfig.ValueRO.ExplosionDeathPrefab;
                    var explosionPosition = localTransform.ValueRW.Position;
                    var newEntity = Ecb.Instantiate(chunkIndex, explosionPrefab);
                    Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(explosionPosition));
                }

                // Also flash the screen when a frigate explodes
                {
                    var newEntity = Ecb.CreateEntity(chunkIndex);
                    Ecb.AddComponent(chunkIndex, newEntity, new EffectCallScreenFlash
                    {
                        Color = new float4(1f, 1f, 1f, 1.0f),
                        Duration = 0.1f,
                    });
                    // Also destroy it using the ToBeDestroyed component so it's hopefully only alive for 1 frame
                    Ecb.AddComponent(chunkIndex, newEntity, new ToBeDestroyed());
                }
            }

        }
    }
}
