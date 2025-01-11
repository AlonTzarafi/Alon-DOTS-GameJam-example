using Unity.Entities;
using Unity.Mathematics;

public struct Explosion : IComponentData
{
    public float ShrinkSpeed;
    public float TimeToLive;

    // For collision detection
    public uint KillCollisionId;
    public float KillCollisionRadius;
    public bool KillLimitToQuadrant;

    // State
    public int DealtDamageCount;
}
