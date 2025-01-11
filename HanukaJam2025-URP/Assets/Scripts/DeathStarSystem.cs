using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public partial struct DeathStarSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Create the singleton DeathStar entity
        var newEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(newEntity, new DeathStar
        {
            Shield = 1f,
            Health = 1f,
        });
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        var hitsArray = new NativeArray<int>(1, Allocator.TempJob);
        hitsArray[0] = 0;

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        var job = new DeathStarUpdateJob()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb,
            Hits = hitsArray,
        };

        // need to finish and extract the Hits value
        job.Schedule(state.Dependency).Complete();
        var hits = hitsArray[0];
        hitsArray.Dispose();
        // UnityEngine.Debug.Log($"DeathStarSystem: {hits} hits");

        var shieldDamagePerHit = 0.0003f;
        var damagePerHit = 0.0007f;
        var shieldRegenSpeed = 0.009f;

        // shieldDamagePerHit *= 10;
        // damagePerHit *= 10;

        var deathStarEntity = SystemAPI.GetSingletonEntity<DeathStar>();
        var deathStar = SystemAPI.GetSingleton<DeathStar>();
        var shield = deathStar.Shield;
        var health = deathStar.Health;

        var shieldHits = 0;
        
        for (int i = 0; i < hits; i++) {
            if (shield > shieldDamagePerHit) {
                shield -= shieldDamagePerHit;
                shieldHits++;
            } else {
                health = math.max(0, health - damagePerHit);
            }
        }

        var stillAlive = health > 0;
        if (stillAlive) {
            // Regenerate shield
            shield = math.min(1, shield + shieldRegenSpeed * SystemAPI.Time.DeltaTime);

            // Update the DeathStar entity
            ecb.SetComponent(100000, deathStarEntity, new DeathStar()
            {
                Shield = shield,
                Health = health,
                Died = false,
                ShieldHits = shieldHits,
            });
        } else {
            // DeathStar is destroyed
            ecb.SetComponent(100000, deathStarEntity, new DeathStar()
            {
                Shield = 0,
                Health = 0,
                Died = true,
                ShieldHits = shieldHits,
            });
        }
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}

[BurstCompile]
public partial struct DeathStarUpdateJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public float DeltaTime;
    public double ElapsedTime;

    public NativeArray<int> Hits;

    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, RefRW<Dancer> dancer, RefRW<LocalTransform> localTransform)
    {
        var deathStarPos = math.float3(0, 0, 94);
        var deathStarRadius = 34f;

        var distance = math.distance(localTransform.ValueRW.Position, deathStarPos);
        // UnityEngine.Debug.Log($"DeathStarSystem: distance={distance}");
        
        var hit = distance < deathStarRadius;
        if (hit) {
            // UnityEngine.Debug.Log($"DeathStarSystem: hit!");
            Ecb.AddComponent(chunkIndex, entity, new ToBeDestroyed());

            // Increment the hits counter
            Hits[0]++;
        }
    }
}
