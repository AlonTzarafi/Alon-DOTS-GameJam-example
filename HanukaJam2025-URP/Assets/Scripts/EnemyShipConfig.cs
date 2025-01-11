using Unity.Entities;
using Unity.Mathematics;

public struct EnemyShipConfig : IComponentData
{
    // Special ships config
    public bool CrazyLaserOrb;
    public Entity CrazyLaserOrbLaserPrefab;
    public Entity CrazyLaserOrbLaserExplosionPrefab;

    // Death config
    public float DeathDelay;
    public Entity DeathDelayPrefab;
    public Entity ExplosionDeathPrefab;
}
