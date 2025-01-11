using Unity.Entities;
using Unity.Mathematics;

public struct EnemyShipDeployment : IComponentData
{
    public float TimeUntilSpawn;
    public int SpawnCount;
}
