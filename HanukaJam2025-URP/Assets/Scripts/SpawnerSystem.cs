// using Unity.Entities;
// using Unity.Transforms;
// using Unity.Burst;
// using Unity.Mathematics;

// [BurstCompile]
// public partial struct SpawnerSystem : ISystem
// {
//     public void OnCreate(ref SystemState state) { }

//     public void OnDestroy(ref SystemState state) { }

//     [BurstCompile]
//     public void OnUpdate(ref SystemState state)
//     {
//         // Queries for all Spawner components. Uses RefRW because this system wants
//         // to read from and write to the component. If the system only needed read-only
//         // access, it would use RefRO instead.
//         foreach (RefRW<Spawner> spawner in SystemAPI.Query<RefRW<Spawner>>())
//         {
//             ProcessSpawner(ref state, spawner);
//         }
//     }

//     private void ProcessSpawner(ref SystemState state, RefRW<Spawner> spawner)
//     {
//         // If the next spawn time has passed.
//         var ElapsedTime = SystemAPI.Time.ElapsedTime;
//         if (spawner.ValueRO.NextSpawnTime < SystemAPI.Time.ElapsedTime)
//         {
//             // Spawns a new entity and positions it at the spawner.
//             Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);
//             // LocalPosition.FromPosition returns a Transform initialized with the given position.
            
//             // Position it around the spawner
//             var spawnPos = spawner.ValueRO.SpawnPosition;
//             // var seed = (uint)(1 + ElapsedTime * 2000000.0 + indexInChunk * 4981219.8);
//             var seed = (uint)(1 + ElapsedTime * 2000000.0);
//             if (seed == 0) {
//                 seed = 1;
//             }
//             var random = new Random(seed);
//             var range = 4;
//             spawnPos += random.NextFloat3(-range, range);
            
//             // state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition));
//             // state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(spawnPos));

//             // Resets the next spawn time.
//             spawner.ValueRW.NextSpawnTime = (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnRate;
//         }
//     }
// }
