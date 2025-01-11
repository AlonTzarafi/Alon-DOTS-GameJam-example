using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using UnityEngine;

public class EnemyStatusAuthoring : MonoBehaviour
{
    public float TotalHealth;
}

class EnemyStatusBaker : Baker<EnemyStatusAuthoring>
{
    public override void Bake(EnemyStatusAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemyStatus
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            TotalHealth = authoring.TotalHealth,
        });
    }
}

