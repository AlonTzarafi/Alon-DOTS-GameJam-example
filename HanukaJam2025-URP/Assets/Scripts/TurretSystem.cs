using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using System;

[BurstCompile]
public partial struct TurretSystem : ISystem
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

        var deathStar = SystemAPI.GetSingleton<DeathStar>();
        var isDeathStarDestroyed = deathStar.Died;

        var clickRate = SystemAPI.GetSingleton<ClickRate>();
        var clickFrenzy = clickRate.ClickFrenzy;

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        var job = new TurretUpdateJob
        {
            DancerTransforms = dancerTransforms,
            DancerEntities = dancerEntities,
            ClickFrenzy = clickFrenzy,
            IsDeathStarDestroyed = isDeathStarDestroyed,
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
public partial struct TurretUpdateJob : IJobEntity
{
    [ReadOnly] public NativeArray<LocalTransform> DancerTransforms;
    [ReadOnly] public NativeArray<Entity> DancerEntities;
    [ReadOnly] public float ClickFrenzy;
    [ReadOnly] public bool IsDeathStarDestroyed;
    
    public float DeltaTime;
    public double ElapsedTime;

    public EntityCommandBuffer.ParallelWriter Ecb;


    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    private void Execute([ChunkIndexInQuery] int chunkIndex, RefRW<Turret> turret, RefRO<Shooter> shooter, RefRO<EnemyStatus> enemyStatus, RefRW<LocalTransform> localTransform)
    {
        if (IsDeathStarDestroyed) {
            return;
        }

        if (enemyStatus.ValueRO.IsTooDamagedToFunction) {
            return;
        }

        var rotationSpeed = turret.ValueRO.RotationSpeed;
        var fireRate = turret.ValueRO.FireRate;

        var hasRotation = rotationSpeed > 0;

        if (turret.ValueRO.CanBeFrenzied) {
            // rotationSpeed += ClickFrenzy * 11.0f;
            fireRate -= ClickFrenzy * 0.24f;
            fireRate = math.max(0.05f, fireRate);
        }

        var turretPosition = localTransform.ValueRO.Position;

        var turretDeadZoneZ = turret.ValueRO.DeadZoneZ;
        var minimumZToTarget = turret.ValueRO.MinimumZToTarget;
        var minZ = -9999f;
        var maxZ = 9999f;
        if (turretDeadZoneZ != 0) {
            // Make a kill Z threshold relative to turretPosition
            maxZ = turretPosition.z - turretDeadZoneZ;
        }
        if (minimumZToTarget != 0) {
            // Pass the value as is - as a minimum Z threshold
            minZ = minimumZToTarget;
        }

        var minMaxZ = new float2(minZ, maxZ);
        Entity currentTarget = Helpers.FindClosestEntity(turretPosition, minMaxZ, DancerEntities, DancerTransforms);
        // Entity currentTarget = Entity.Null;
        turret.ValueRW.Target = currentTarget;

        var hasTarget = currentTarget != Entity.Null;
        if (hasRotation && hasTarget) {
            // UnityEngine.Debug.Log($"currentTarget: {currentTarget}");
            var targetTransform = DancerTransforms[DancerEntities.IndexOf(currentTarget)];
            var targetPosition = targetTransform.Position;
            var targetRotation = quaternion.LookRotationSafe(targetPosition - turretPosition, math.up());
            localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRW.Rotation, targetRotation, rotationSpeed * DeltaTime);
        }
        

        turret.ValueRW.TimeUntilFire -= DeltaTime;
        if (turret.ValueRW.TimeUntilFire <= 0)
        {
            turret.ValueRW.TimeUntilFire = fireRate;

            Helpers.Shoot(Ecb, chunkIndex, shooter.ValueRO, localTransform.ValueRO);
        }

        // localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRW.Rotation, targetRotation, t);
    }
}
