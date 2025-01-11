using Unity.Entities;
using Unity.Mathematics;

public struct Projectile : IComponentData
{
    // For Travel
    public float MovementSpeed;

    // For collision detection
    public float CollisionRadius;
    public uint CollisionId;

    // For hitting
    public Entity PrefabOnHit;
    public bool PrefabOnHitRandomRotation;

    // For disposal
    public float MinZ;
    public float MaxZ;
}
