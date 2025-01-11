using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using UnityEngine;

class GameConfigAuthoring : MonoBehaviour
{
    public float EnemyShipDeploymentTime;
    public GameObject EnemyShip1Prefab;
    public GameObject EnemyShip2Prefab;
    public GameObject PlayerClickIndicatorPrefab;
}

class GameConfigBaker : Baker<GameConfigAuthoring>
{
    public override void Bake(GameConfigAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new GameConfig
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            EnemyShipDeploymentTime = authoring.EnemyShipDeploymentTime,
            EnemyShip1Prefab = GetEntity(authoring.EnemyShip1Prefab, TransformUsageFlags.Dynamic),
            EnemyShip2Prefab = GetEntity(authoring.EnemyShip2Prefab, TransformUsageFlags.Dynamic),
            PlayerClickIndicatorPrefab = GetEntity(authoring.PlayerClickIndicatorPrefab, TransformUsageFlags.Dynamic),
        });
    }
}
