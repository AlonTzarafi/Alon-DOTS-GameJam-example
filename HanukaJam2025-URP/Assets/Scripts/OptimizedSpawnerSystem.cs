using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public partial struct OptimizedSpawnerSystem : ISystem
{
    private EntityQuery _allShipsQuery;

    public void OnCreate(ref SystemState state)
    {
        // Make SpawningExtra singleton
        var newEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(newEntity, new SpawningExtra
        {
            ForceSpawnsAtFirstFrames = 100,
        });

        _allShipsQuery = state.EntityManager.CreateEntityQuery(typeof(Dancer));
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var forceSpawnLeft = false;
        var spawningExtra = SystemAPI.GetSingleton<SpawningExtra>();
        var forceSpawnsAtFirstFrames = spawningExtra.ForceSpawnsAtFirstFrames;
        if (forceSpawnsAtFirstFrames > 0) {
            forceSpawnsAtFirstFrames--;
            forceSpawnLeft = true;
        }
        spawningExtra.ForceSpawnsAtFirstFrames = forceSpawnsAtFirstFrames;
        SystemAPI.SetSingleton(spawningExtra);

        var totalSpawnedCount = _allShipsQuery.CalculateEntityCount();

        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        new ProcessSpawnerJob
        {
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            ForceSpawnLeft = forceSpawnLeft,
            TotalSpawnedCount = totalSpawnedCount,
            Ecb = ecb
        }
        // .Schedule();
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
public partial struct ProcessSpawnerJob : IJobEntity
{
    public bool ForceSpawnLeft;
    public double ElapsedTime;
    public int TotalSpawnedCount;

    public EntityCommandBuffer.ParallelWriter Ecb;

    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    private void Execute([ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, ref Spawner spawner)
    {
        if (TotalSpawnedCount > 22000) {
            return;
        }
        
        // If the next spawn time has passed.
        if (ForceSpawnLeft || spawner.NextSpawnTime < ElapsedTime)
        {
            // Spawn a new entity
            Entity newEntity = Ecb.Instantiate(chunkIndex, spawner.Prefab);

            // Position it around the spawner
            var spawnPos = spawner.SpawnPosition;
            var seed = (uint)(1 + ElapsedTime * 2000000.0 + indexInChunk * 4981219.8);
            if (seed == 0) {
                seed = 1;
            }
            var random = new Random(seed);
            var range = random.NextFloat(7f, 22f);
            var xScale = 1.4f;
            var dist = range;
            var angle = random.NextFloat(0f, 2f * math.PI);
            spawnPos.x = dist * math.cos(angle) * xScale;
            spawnPos.y = dist * math.sin(angle);
            // spawnPos.z = -10f;
            // spawnPos.z = -4f;
            spawnPos.z = random.NextFloat(-10f, -4f);
            // spawnPos.z = random.NextFloat(-10f, -10f);
            Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(spawnPos));

            // // choose 0-20 for chunking
            // var randomValue = random.NextInt(0, 20);
            // Ecb.AddSharedComponent(chunkIndex, newEntity, new Chunker { Value = randomValue });

            // var speed = random.NextFloat(0.5f, 1.5f);
            var speed = random.NextFloat(0.2f, 1.1f);
            var destinationRange = 15f;
            var destinationX = random.NextFloat(-destinationRange, destinationRange);
            var destinationY = random.NextFloat(-destinationRange, destinationRange);
            var destinationZ = random.NextFloat(85f, 90f);

            // Z range of spawining is: -10f to -4f
            // Z of actually starting to be shown on camera is: 1f to 10f
            var fastUntilZ = -9999f;
            if (random.NextInt(100) < 60) {
                fastUntilZ = random.NextFloat(-10f, 10f);
            }
            

            Ecb.SetComponent(chunkIndex, newEntity, new Dancer
            {
                Speed = speed,
                FastUntilZ = fastUntilZ,
                NextPos = new float3(destinationX, destinationY, destinationZ)
            });

            // Resets the next spawn time
            spawner.NextSpawnTime = (float)ElapsedTime + spawner.SpawnRate;
        }
    }
}
