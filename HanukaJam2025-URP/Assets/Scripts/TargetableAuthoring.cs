using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

class TargetableAuthoring : MonoBehaviour
{
    // For collision detection
    public float CollisionRadius;
    public CollisionId CollisionId;
}

class TargetableBaker : Baker<TargetableAuthoring>
{
    public override void Bake(TargetableAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new Targetable
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            CollisionRadius = authoring.CollisionRadius,
            CollisionId = (uint)authoring.CollisionId,
        });
    }
}
