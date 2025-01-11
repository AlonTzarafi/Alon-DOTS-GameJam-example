using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using System;

public static class Helpers
{
    public static Entity FindClosestEntity(float3 seekerPosition, float2 minMaxZ, NativeArray<Entity> entities, NativeArray<LocalTransform> transforms)
    {
        Entity closestEntity = Entity.Null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < entities.Length; i++)
        {
            var minZ = minMaxZ.x;
            var maxZ = minMaxZ.y;
            if (transforms[i].Position.z < minZ || transforms[i].Position.z > maxZ) {
                continue;
            }
            
            var targetPosition = transforms[i].Position;
            var distance = math.distance(seekerPosition, targetPosition);
            if (distance < closestDistance) {
                closestDistance = distance;
                closestEntity = entities[i];
            }
        }

        return closestEntity;
    }

    public static int FindClosestEntityIndex(float3 turretPosition, float2 minMaxZ, NativeArray<LocalTransform> dancerTransforms)
    {
        var closestIndex = -1;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < dancerTransforms.Length; i++)
        {
            var minZ = minMaxZ.x;
            var maxZ = minMaxZ.y;
            if (dancerTransforms[i].Position.z < minZ || dancerTransforms[i].Position.z > maxZ) {
                continue;
            }
            
            
            var dancerPosition = dancerTransforms[i].Position;
            var distance = math.distance(turretPosition, dancerPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    static public void Shoot(EntityCommandBuffer.ParallelWriter ecb, int chunkIndex, Shooter shooter, LocalTransform localTransform)
    {
        var prefab = shooter.Prefab;
        var shootPoint1 = shooter.ShootPoint1;
        var useSecondShoot = shooter.UseSecondShoot;
        var shootPoint2 = shooter.ShootPoint2;

        var rotation = localTransform.Rotation;
        // rotation = quaternion.identity;
        var position = localTransform.Position;

        // For created Projectile:
        var movementDirection = math.mul(rotation, new float3(0, 0, 1));

        // Instantiate a projectile
        var newEntity = ecb.Instantiate(chunkIndex, prefab);

        var spawnPos1 = position + math.mul(rotation, shootPoint1);
        var spawnPos2 = position + math.mul(rotation, shootPoint2);
        ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(spawnPos1));

        // Add MovementDirection component
        ecb.AddComponent(chunkIndex, newEntity, new MovementDirection { Value = movementDirection });


        if (useSecondShoot) {
            var newEntity2 = ecb.Instantiate(chunkIndex, prefab);
            ecb.SetComponent(chunkIndex, newEntity2, LocalTransform.FromPosition(spawnPos2));
            ecb.AddComponent(chunkIndex, newEntity2, new MovementDirection { Value = movementDirection });
        }
        if (shooter.MultipleShots) {
            var distance = shooter.MultipleShotsDistance;
            var pos1 = spawnPos1 + new float3(distance, 0, 0);
            var pos2 = spawnPos1 + new float3(-distance, 0, 0);
            var pos3 = spawnPos1 + new float3(0, distance, 0);
            var pos4 = spawnPos1 + new float3(0, -distance, 0);
            var newEntityMultiple1 = ecb.Instantiate(chunkIndex, prefab);
            ecb.SetComponent(chunkIndex, newEntityMultiple1, LocalTransform.FromPosition(pos1));
            ecb.AddComponent(chunkIndex, newEntityMultiple1, new MovementDirection { Value = movementDirection });
            var newEntityMultiple2 = ecb.Instantiate(chunkIndex, prefab);
            ecb.SetComponent(chunkIndex, newEntityMultiple2, LocalTransform.FromPosition(pos2));
            ecb.AddComponent(chunkIndex, newEntityMultiple2, new MovementDirection { Value = movementDirection });
            var newEntityMultiple3 = ecb.Instantiate(chunkIndex, prefab);
            ecb.SetComponent(chunkIndex, newEntityMultiple3, LocalTransform.FromPosition(pos3));
            ecb.AddComponent(chunkIndex, newEntityMultiple3, new MovementDirection { Value = movementDirection });
            var newEntityMultiple4 = ecb.Instantiate(chunkIndex, prefab);
            ecb.SetComponent(chunkIndex, newEntityMultiple4, LocalTransform.FromPosition(pos4));
            ecb.AddComponent(chunkIndex, newEntityMultiple4, new MovementDirection { Value = movementDirection });
        }
    }

    static public bool CanCollideWithAnything(uint collisionId)
    {
        return (CollisionId)collisionId != CollisionId.None;
    }
}
