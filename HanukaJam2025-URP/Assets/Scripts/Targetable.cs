using Unity.Entities;
using Unity.Mathematics;

public struct Targetable : IComponentData
{
    // For collision detection
    public float CollisionRadius;
    public uint CollisionId;
}
