using Unity.Entities;
using Unity.Mathematics;

// A singleton component storing the latest input
public struct GameInputData : IComponentData
{
    public bool AllowInput;

    public float2 Aim;
    public bool Click;
}
