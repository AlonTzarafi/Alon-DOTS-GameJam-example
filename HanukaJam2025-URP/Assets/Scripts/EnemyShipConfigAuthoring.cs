using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using UnityEngine;

public class EnemyShipConfigAuthoring : MonoBehaviour
{
    // Special ships config
    public bool CrazyLaserOrb;
    public GameObject CrazyLaserOrbLaserPrefab;
    public GameObject CrazyLaserOrbLaserExplosionPrefab;

    // Death config
    public float DeathDelay;
    public GameObject DeathDelayPrefab;
    public GameObject ExplosionDeathPrefab;
}

class EnemyShipConfigBaker : Baker<EnemyShipConfigAuthoring>
{
    public override void Bake(EnemyShipConfigAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemyShipConfig
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            CrazyLaserOrb = authoring.CrazyLaserOrb,
            CrazyLaserOrbLaserPrefab = GetEntity(authoring.CrazyLaserOrbLaserPrefab, TransformUsageFlags.Dynamic),
            CrazyLaserOrbLaserExplosionPrefab = GetEntity(authoring.CrazyLaserOrbLaserExplosionPrefab, TransformUsageFlags.Dynamic),
            DeathDelay = authoring.DeathDelay,
            DeathDelayPrefab = GetEntity(authoring.DeathDelayPrefab, TransformUsageFlags.Dynamic),
            ExplosionDeathPrefab = GetEntity(authoring.ExplosionDeathPrefab, TransformUsageFlags.Dynamic),
        });
    }
}

