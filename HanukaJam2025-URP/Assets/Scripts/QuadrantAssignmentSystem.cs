using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms; // for LocalTransform
using Unity.Jobs;
using Unity.Mathematics;


public static class QuadrantUtils
{
    // Define a bounding region in 3D space
    public readonly static float3 BOUNDS_MIN = new float3(-50f, -50f, -30f);
    public readonly static float3 BOUNDS_MAX = new float3( 50f,  50f,  100f);

    // How many subdivisions (cells) along each axis?
    public const int SPLIT_X = 20;
    public const int SPLIT_Y = 20;
    public const int SPLIT_Z = 20;

    public static float3 BoundsSize => BOUNDS_MAX - BOUNDS_MIN;

    /// <summary>
    /// Returns a flattened 3D quadrant index based on position.
    /// </summary>
    public static int GetQuadrantIndex(float3 position)
    {
        // Clamp entity's position into our bounding box
        float3 clamped = math.clamp(position, BOUNDS_MIN, BOUNDS_MAX) - BOUNDS_MIN;
        float3 normalized = clamped / BoundsSize;  // range [0..1] in each axis

        // Compute integer indices in X, Y, Z
        int xIndex = (int)(normalized.x * SPLIT_X);
        int yIndex = (int)(normalized.y * SPLIT_Y);
        int zIndex = (int)(normalized.z * SPLIT_Z);

        // Ensure we don't go out of bounds (e.g. 4 if normalized=1.0)
        xIndex = math.min(xIndex, SPLIT_X - 1);
        yIndex = math.min(yIndex, SPLIT_Y - 1);
        zIndex = math.min(zIndex, SPLIT_Z - 1);

        // Flatten (x,y,z) into a single index
        // Example: quadrantIndex = z * (SPLIT_X * SPLIT_Y) + y * SPLIT_X + x
        return xIndex 
             + yIndex * SPLIT_X 
             + zIndex * SPLIT_X * SPLIT_Y;
    }
}


[BurstCompile]
public partial struct QuadrantAssignmentSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var ecbParallel = ecb.AsParallelWriter();

        var handle = new QuadrantAssignmentJob
        {
            Ecb = ecbParallel
        }.ScheduleParallel(state.Dependency);

        handle.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    partial struct QuadrantAssignmentJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, RefRW<Quadrant> quadrant, RefRO<LocalTransform> transform)
        {
            var shouldUpdate = Quadrant.ShouldUpdate(quadrant.ValueRW.UpdateMode);
            if (!shouldUpdate)
                return;

            int newIndex = QuadrantUtils.GetQuadrantIndex(transform.ValueRO.Position);

            quadrant.ValueRW.Index = newIndex;
            quadrant.ValueRW.UpdateMode = Quadrant.AfterUpdate(quadrant.ValueRW.UpdateMode);
        }
    }
}
