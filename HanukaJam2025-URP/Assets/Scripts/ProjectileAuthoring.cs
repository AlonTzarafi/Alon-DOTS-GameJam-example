using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

class ProjectileAuthoring : MonoBehaviour
{
    public float MovementSpeed;
    
    // For collision detection
    public float CollisionRadius;
    public CollisionId CollisionId;

    // For hitting
    public GameObject PrefabOnHit;
    public bool PrefabOnHitRandomRotation;

    // For disposal
    public float MinZ = -100.0f;
    public float MaxZ = 300.0f;
}

class ProjectileBaker : Baker<ProjectileAuthoring>
{
    public override void Bake(ProjectileAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Projectile
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            MovementSpeed = authoring.MovementSpeed,
            CollisionRadius = authoring.CollisionRadius,
            CollisionId = (uint)authoring.CollisionId,
            PrefabOnHit = GetEntity(authoring.PrefabOnHit, TransformUsageFlags.Dynamic),
            PrefabOnHitRandomRotation = authoring.PrefabOnHitRandomRotation,
            MinZ = authoring.MinZ,
            MaxZ = authoring.MaxZ
        });
    }
}
