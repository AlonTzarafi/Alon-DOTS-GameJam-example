using Unity.Entities;
using Unity.Mathematics;

public struct EnemyShip : IComponentData
{
    public int SeekRandomTargetIndex;
    public float SeekRandomTargetDelay;
    public float SeekRandomTargetTimer;
    public Entity Target;
    public float3 TargetPosition;
    public float Spin;
    public float LaserCycle;
    public float LaserCooldown;
    
    public float TimeSpentAtZeroHealth;
}
