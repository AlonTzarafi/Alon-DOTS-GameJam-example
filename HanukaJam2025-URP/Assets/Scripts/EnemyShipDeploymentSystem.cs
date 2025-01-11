using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;

[BurstCompile]
public partial struct EnemyShipDeploymentSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var newEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(newEntity, new EnemyShipDeployment
        {
            TimeUntilSpawn = 1.0f,
        });
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        new EnemyDeploymentJob
        {
            GameConfig = SystemAPI.GetSingleton<GameConfig>(),
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
public partial struct EnemyDeploymentJob : IJobEntity
{
    public GameConfig GameConfig;
    public float DeltaTime;
    public double ElapsedTime;

    public EntityCommandBuffer.ParallelWriter Ecb;

    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, RefRW<EnemyShipDeployment> enemyShipDeployment)
    {
        enemyShipDeployment.ValueRW.TimeUntilSpawn -= DeltaTime;
        var timeUntilSpawn = enemyShipDeployment.ValueRW.TimeUntilSpawn;
        if (timeUntilSpawn <= 0.0f) {
            var seed = (uint)(1 + ElapsedTime * 2000000.0 + enemyShipDeployment.ValueRW.SpawnCount * 4981219.8);
            var random = new Random(seed);

            timeUntilSpawn = GameConfig.EnemyShipDeploymentTime;
            enemyShipDeployment.ValueRW.TimeUntilSpawn = timeUntilSpawn;
            enemyShipDeployment.ValueRW.SpawnCount++;

            var prefabToInstantiate = GameConfig.EnemyShip1Prefab;
            if (enemyShipDeployment.ValueRW.SpawnCount % 2 == 1) {
                prefabToInstantiate = GameConfig.EnemyShip2Prefab;
            }
            var newEntity = Ecb.Instantiate(chunkIndex, prefabToInstantiate);
            float3 position;
            // var dist = 50f + (5f * enemyShipDeployment.ValueRW.SpawnCount);
            var sidewaysDist = 38.32f;
            // if ((enemyShipDeployment.ValueRW.SpawnCount / 2) % 2 != 0) {
            if (random.NextBool()) {
                sidewaysDist = -sidewaysDist;
            }

            // Randomize it a bit from -20 to 20
            var verticalDist = random.NextFloat(-20f, 20f);
            
            position = new float3(sidewaysDist, verticalDist, 120);

            Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(position));

            var delayBetweenTargetSeeking = 0.2f;
            Ecb.AddComponent(chunkIndex, newEntity, new EnemyShip
            {
                SeekRandomTargetIndex = random.NextInt(0, 10000),
                SeekRandomTargetDelay = random.NextFloat(delayBetweenTargetSeeking, delayBetweenTargetSeeking),
                SeekRandomTargetTimer = random.NextFloat(0.0f, delayBetweenTargetSeeking),
                Target = Entity.Null,
                Spin = 0.0f
            });
            
            Ecb.AddBuffer<DamageInstance>(chunkIndex, newEntity);
        }
    }
}
