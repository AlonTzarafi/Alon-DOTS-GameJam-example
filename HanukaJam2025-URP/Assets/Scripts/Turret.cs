using Unity.Entities;
using Unity.Mathematics;

public struct Turret : IComponentData
{
    // Config
    public float FireRate;
    public float DeadZoneZ;
    public float MinimumZToTarget;
    public float RotationSpeed;
    public bool CanBeFrenzied;

    // State
    public Entity Target;
    public float TimeUntilFire;
}
