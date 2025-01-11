using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

class QuadrantAuthoring : MonoBehaviour
{
    public QuadrantUpdateModeForAuthoring UpdateMode = QuadrantUpdateModeForAuthoring.Continuous;
}

class QuadrantBaker : Baker<QuadrantAuthoring>
{
    public override void Bake(QuadrantAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Quadrant
        {
            UpdateMode = (QuadrantUpdateMode)authoring.UpdateMode,
        });
    }
}
