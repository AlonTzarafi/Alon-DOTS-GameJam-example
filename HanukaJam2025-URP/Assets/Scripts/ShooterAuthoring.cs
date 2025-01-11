using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

class ShooterAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float3 ShootPoint1;

    // Optional:
    public bool UseSecondShoot;
    public float3 ShootPoint2;
    public bool MultipleShots;
    public float MultipleShotsDistance;
}

class ShooterBaker : Baker<ShooterAuthoring>
{
    public override void Bake(ShooterAuthoring authoring)
    {
        DependsOn(authoring.Prefab);

        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Shooter
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
            ShootPoint1 = authoring.ShootPoint1,
            UseSecondShoot = authoring.UseSecondShoot,
            ShootPoint2 = authoring.ShootPoint2,
            MultipleShots = authoring.MultipleShots,
            MultipleShotsDistance = authoring.MultipleShotsDistance,
        });
    }
}
