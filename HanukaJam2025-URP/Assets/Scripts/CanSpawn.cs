using Unity.Entities;
using Unity.Mathematics;

public struct CanSpawn : IComponentData
{
    public Entity Prefab;
    public Entity Prefab2;

    
    public Entity AnotherPrefab;
}
