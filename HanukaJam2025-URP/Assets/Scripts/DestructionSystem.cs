using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using System;


[BurstCompile]
// [UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct DestructionSystem : ISystem
{
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        var job = new DestructionUpdateJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb
        };

        // Schedule the job and retrieve the JobHandle
        var jobHandle = job.ScheduleParallel(state.Dependency);

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
public partial struct DestructionUpdateJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public float DeltaTime;
    public double ElapsedTime;

    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, RefRW<ToBeDestroyed> toBeDestroyed)
    {
        if (toBeDestroyed.ValueRW.CyclesElapsed > 0) {
            Ecb.RemoveComponent<ToBeDestroyed>(chunkIndex, entity);
        }
        toBeDestroyed.ValueRW.CyclesElapsed += 1;
        
        Ecb.DestroyEntity(chunkIndex, entity);
    }
}
