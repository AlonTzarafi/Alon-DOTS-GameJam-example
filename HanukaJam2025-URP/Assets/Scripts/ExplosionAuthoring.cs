using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

class ExplosionAuthoring : MonoBehaviour
{
    public float ShrinkSpeed;
    public float TimeToLive;

    // For collision detection
    public CollisionId KillCollisionId;
    public float KillCollisionRadius;
    public bool KillLimitToQuadrant;
}

class ExplosionBaker : Baker<ExplosionAuthoring>
{
    public override void Bake(ExplosionAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Explosion
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            // CollisionRadius = authoring.CollisionRadius,
            ShrinkSpeed = authoring.ShrinkSpeed,
            TimeToLive = authoring.TimeToLive,
            KillCollisionId = (uint)authoring.KillCollisionId,
            KillCollisionRadius = authoring.KillCollisionRadius,
            KillLimitToQuadrant = authoring.KillLimitToQuadrant,
        });
    }
}
