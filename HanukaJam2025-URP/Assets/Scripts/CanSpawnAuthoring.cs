using UnityEngine;
using Unity.Entities;

class CanSpawnAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public GameObject Prefab2;
    public GameObject AnotherPrefab;
}

class CanSpawnBaker : Baker<CanSpawnAuthoring>
{
    public override void Bake(CanSpawnAuthoring authoring)
    {
        DependsOn(authoring.Prefab);
        DependsOn(authoring.Prefab2);
        DependsOn(authoring.AnotherPrefab);

        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new CanSpawn
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
            Prefab2 = GetEntity(authoring.Prefab2, TransformUsageFlags.Dynamic),
            AnotherPrefab = GetEntity(authoring.AnotherPrefab, TransformUsageFlags.Dynamic),
        });
    }
}
