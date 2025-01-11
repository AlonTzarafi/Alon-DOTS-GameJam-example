using UnityEngine;
using Unity.Entities;

class TurretAuthoring : MonoBehaviour
{
    public float FireRate;
    public float DeadZoneZ;
    public float MinimumZToTarget;
    public float RotationSpeed;
    public bool CanBeFrenzied;
}

class TurretBaker : Baker<TurretAuthoring>
{
    public override void Bake(TurretAuthoring authoring)
    {
        // var entity = GetEntity(TransformUsageFlags.None);
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Turret
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            FireRate = authoring.FireRate,
            DeadZoneZ = authoring.DeadZoneZ,
            MinimumZToTarget = authoring.MinimumZToTarget,
            RotationSpeed = authoring.RotationSpeed,
            CanBeFrenzied = authoring.CanBeFrenzied,
        });
    }
}
