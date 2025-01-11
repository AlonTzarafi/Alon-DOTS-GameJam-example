using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(BoosterSystem))]
[BurstCompile]
public partial struct DancerSystem : ISystem
{
    public void OnCreate(ref SystemState state) { }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        new DancerUpdateJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb
        }
        .ScheduleParallel();
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}

[BurstCompile]
public partial struct DancerUpdateJob : IJobEntity
{
    public float DeltaTime;
    public double ElapsedTime;

    public EntityCommandBuffer.ParallelWriter Ecb;

    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    // private void Execute(ref Dancer dancer, ref LocalToWorld localToWorld)
    // private void Execute(ref LocalToWorld localToWorld)
    private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, RefRW<Dancer> dancer, RefRW<LocalTransform> localTransform)
    {
        var speed = dancer.ValueRO.Speed;
        var fastUntilZ = dancer.ValueRO.FastUntilZ;
        var isFast = localTransform.ValueRW.Position.z < fastUntilZ;
        if (isFast) {
            speed *= 8.0f;
        }

        var pos = localTransform.ValueRW.Position;
        // var rotation = localToWorld.ValueRW.Rotation;
        // var scale = localToWorld.ValueRW.Value.Scale();
        var nextPos = dancer.ValueRO.NextPos;
        var toNextPos = nextPos - pos;
        var norm = math.normalizesafe(toNextPos);
        var distToMove = speed * DeltaTime;
        distToMove = math.min(distToMove, math.length(toNextPos));
        var movement = norm * distToMove;
        var finalPos = pos + movement;
        localTransform.ValueRW.Position = finalPos;

        if (!dancer.ValueRW.InitializedRotation) {
            // Rotate towards target
            var targetRotation = quaternion.LookRotation(movement, math.up());
            var rotationLerpSpeed = 500.0f;
            var t = math.saturate(DeltaTime * rotationLerpSpeed);
            localTransform.ValueRW.Rotation = math.slerp(localTransform.ValueRW.Rotation, targetRotation, t);

            dancer.ValueRW.InitializedRotation = true;
        }

        // var reachedVeryFar = finalPos.z > 300.0f;
        var reachedVeryFar = finalPos.z >= MouseRaycastSystem.TOO_FAR_AWAY_PLANE_DISTANCE;
        if (reachedVeryFar) {
            Ecb.AddComponent(chunkIndex, entity, new ToBeDestroyed());
        }
    }
}
