using Unity.Entities;
using Unity.Mathematics;

public struct EffectCallScreenFlash : IComponentData
{
    public float4 Color;
    public float Duration;
}
