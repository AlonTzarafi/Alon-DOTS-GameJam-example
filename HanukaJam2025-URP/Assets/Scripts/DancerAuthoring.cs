using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

class DancerAuthoring : MonoBehaviour
{
    public float Speed;
    public float3 NextPos;
}

class DancerBaker : Baker<DancerAuthoring>
{
    public override void Bake(DancerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new Dancer
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            Speed = authoring.Speed,
            NextPos = authoring.NextPos,
        });
    }
}
