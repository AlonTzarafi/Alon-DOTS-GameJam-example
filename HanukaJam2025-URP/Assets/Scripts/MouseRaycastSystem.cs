using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using System;
using UnityEngine.InputSystem;
using UnityEngine;
using Unity.Jobs;

[BurstCompile]
public partial struct MouseRaycastSystem : ISystem
{
    public const float TOO_FAR_AWAY_PLANE_DISTANCE = 80f;
    public const float CURSOR_PRESS_PLANE_DISTANCE = 80f;
    public const float CURSOR_PRESS_PLANE_THICKNESS_X = 92f;
    public const float CURSOR_PRESS_PLANE_THICKNESS_Y = 52f;

    public const int MAX_BOOSTERS = 300;
    // public const int MAX_BOOSTERS_FAR_AWAY = 1;
    public const int MAX_BOOSTERS_FAR_AWAY = 0;
    // public const float MAX_DISTANCE = 72f;
    public const float MAX_DISTANCE = 9999f;
    // public const float MAX_SQUADRON_DISTANCE = 12f;
    public const float MAX_SQUADRON_DISTANCE = 52f;

    // Regular ship speed:
    // var speed = random.NextFloat(0.5f, 1.5f);
    public const float BOOSTER_SPEED = 24;
    public const float BOOSTER_SPEED_TO_SQUADRON_ORIGINAL_CENTER = 12;
    public const float BOOSTER_CHASE_SPEED = 12;

    private EntityQuery query;
    private EntityQuery cameraDataQuery;
    
    public void OnCreate(ref SystemState state)
    {
        // Create ClickRate singleton
        state.EntityManager.CreateEntity(typeof(ClickRate));

        // Query for any entity that has Dancer component, but doesn't have a Booster component
        query = SystemAPI.QueryBuilder()
            .WithAll<Dancer, LocalTransform>()
            .WithNone<Booster>()
            .Build();

        cameraDataQuery = state.GetEntityQuery(typeof(CameraData));
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (cameraDataQuery.IsEmpty)
            return;

        var cameraData = cameraDataQuery.GetSingleton<CameraData>();
        var inputData = SystemAPI.GetSingleton<GameInputData>();
        var gameConfig = SystemAPI.GetSingleton<GameConfig>();

        // Debugging
        // Debug.Log($"Aim: {inputData.Aim}");
        // Debug.Log($"Click: {inputData.Click}");

        var aimX = inputData.Aim.x;
        var aimY = inputData.Aim.y;
        var aimingOnScreen = aimX >= -1 && aimX <= 1 && aimY >= -1 && aimY <= 1;

        var worldSpaceAim2D = inputData.Aim * math.float2(CURSOR_PRESS_PLANE_THICKNESS_X, CURSOR_PRESS_PLANE_THICKNESS_Y);
        var worldSpaceAim3D = new float3(worldSpaceAim2D, CURSOR_PRESS_PLANE_DISTANCE);

        var worldTargetPosition = worldSpaceAim3D;

        // Get the transforms and entities for the Dancer-without-Booster group
        var targetsTransforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var targetsEntities = query.ToEntityArray(Allocator.TempJob);
        
        if (inputData.AllowInput && inputData.Click && aimingOnScreen)
        {
            var job = new MouseRaycastJob
            {
                GameConfig = gameConfig,
                WorldTargetPosition = worldTargetPosition,
                Transforms = targetsTransforms,
                Entities = targetsEntities,
                ViewProj = cameraData.ViewProjectionMatrix,
                ScreenSize = cameraData.ScreenSize,
                Ecb = GetEntityCommandBuffer(ref state),
            };
            
            // Complete on the main thread (for simplicity).
            job.Run();
        }

        // Must dispose the arrays after job
        targetsTransforms.Dispose();
        targetsEntities.Dispose();


        var clickRate = SystemAPI.GetSingleton<ClickRate>();
        if (inputData.Click) {
            clickRate.ClickFrenzy += 0.4f;
        }
        clickRate.ClickFrenzy = math.lerp(clickRate.ClickFrenzy, 0, SystemAPI.Time.DeltaTime * 0.8f);
        SystemAPI.SetSingleton(clickRate);
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}

// Helper struct
public struct DistanceEntity : IComparable<DistanceEntity>
{
    public float distance;
    public float3 position;
    public int index;

    public int CompareTo(DistanceEntity other)
    {
        // Sort ascending by distance
        return distance.CompareTo(other.distance);
    }
}

[BurstCompile]
public struct MouseRaycastJob : IJob
{
    [ReadOnly] public GameConfig GameConfig;
    [ReadOnly] public float3 WorldTargetPosition;
    [ReadOnly] public NativeArray<LocalTransform> Transforms;
    [ReadOnly] public NativeArray<Entity> Entities;
    [ReadOnly] public float4x4 ViewProj;
    [ReadOnly] public float2 ScreenSize;

    public EntityCommandBuffer.ParallelWriter Ecb;

    public void Execute()
    {
        int length = Transforms.Length;

        NativeArray<DistanceEntity> distanceArray = new NativeArray<DistanceEntity>(length, Allocator.Temp);

        for (int i = 0; i < length; i++) {
            // var findClosestLocation = WorldTargetPosition;
            // var findClosestLocation = math.float3(0, 0, 80);
            var findClosestLocation = math.lerp(WorldTargetPosition, math.float3(0, 0, 80), 0.8f);

            float dist = math.distance(findClosestLocation, Transforms[i].Position);
            
            distanceArray[i] = new DistanceEntity
            {
                distance = dist,
                position = Transforms[i].Position,
                index = i
            };
        }

        // Sort ascending
        distanceArray.Sort();

        int capacityForFarAwayOnes = math.min(MouseRaycastSystem.MAX_BOOSTERS_FAR_AWAY, length);
        int limit = math.min(MouseRaycastSystem.MAX_BOOSTERS, length);

        // To store the final indices
        NativeList<int> squadIndices = new NativeList<int>(Allocator.Temp);

        bool hasSquadronLeader = false;
        float3 squadronLeaderPosition = float3.zero;

        
        float3 sumPositions = float3.zero;
        int squadCount = 0;

        for (int i = 0; i < limit; i++) {
            var distEnt = distanceArray[i];
            float distance = distEnt.distance;
            int entityIndex = distEnt.index;

            // var validZ = distEnt.position.z > -10;
            var validZ = distEnt.position.z > 0;
            if (!validZ) {
                // Make sure we can only launch ships which are actually visible on the screen...
                continue;
            }

            // If dancer is too far, see if we can allow any "far away" ones
            if (distance > MouseRaycastSystem.MAX_DISTANCE) {
                if (capacityForFarAwayOnes > 0) {
                    capacityForFarAwayOnes--;
                } else {
                    continue;
                }
            }

            if (hasSquadronLeader) {
                float distanceToSquadLeader = math.distance(squadronLeaderPosition, distEnt.position);
                if (distanceToSquadLeader > MouseRaycastSystem.MAX_SQUADRON_DISTANCE) {
                    continue;
                }
            }

            if (!hasSquadronLeader) {
                hasSquadronLeader = true;
                squadronLeaderPosition = distEnt.position;
            }

            squadIndices.Add(entityIndex);
            sumPositions += Transforms[entityIndex].Position;
            squadCount++;
        }

        float3 squadronOriginalCenter = float3.zero;
        if (squadCount > 0) {
            squadronOriginalCenter = sumPositions / squadCount;
        }

        var cameraPosition = math.float3(0, 0, -10);
        squadronOriginalCenter = math.lerp(squadronOriginalCenter, cameraPosition, 0.75f);

        for (int i = 0; i < squadIndices.Length; i++) {
            int entityIndex = squadIndices[i];
            Ecb.AddComponent<Booster>(entityIndex, Entities[entityIndex], new Booster()
            {
                SquadronOriginalCenter = squadronOriginalCenter,
                Target = WorldTargetPosition,
                SpeedToSquadronOriginalCenter = MouseRaycastSystem.BOOSTER_SPEED_TO_SQUADRON_ORIGINAL_CENTER,
                Speed = MouseRaycastSystem.BOOSTER_SPEED,
                TimeUntilFart = (i * 0.01f) % Booster.FART_RATE,
            });
        }

        // Clean up
        squadIndices.Dispose();
        distanceArray.Dispose();

        // Spawn PlayerClickIndicatorPrefab
        var theTargetPosition = WorldTargetPosition;
        var newEntity = Ecb.Instantiate(0, GameConfig.PlayerClickIndicatorPrefab);
        Ecb.SetComponent(0, newEntity, LocalTransform.FromPosition(theTargetPosition));
    }
}
