using Unity.Entities;
using Unity.Mathematics;

public struct Shooter : IComponentData
{
    public Entity Prefab;
    public float3 ShootPoint1;

    // Optional:
    public bool UseSecondShoot;
    public float3 ShootPoint2;
    public bool MultipleShots;
    public float MultipleShotsDistance;
}
