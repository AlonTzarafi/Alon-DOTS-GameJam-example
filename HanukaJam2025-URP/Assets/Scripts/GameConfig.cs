using Unity.Entities;

public struct GameConfig : IComponentData
{
    public float EnemyShipDeploymentTime;
    public Entity EnemyShip1Prefab;
    public Entity EnemyShip2Prefab;

    public Entity PlayerClickIndicatorPrefab;
}
