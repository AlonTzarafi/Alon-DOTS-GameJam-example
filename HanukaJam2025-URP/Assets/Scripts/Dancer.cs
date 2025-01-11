using Unity.Entities;
using Unity.Mathematics;

public struct Dancer : IComponentData
{
    public float Speed;
    public float FastUntilZ;
    public float3 NextPos;
    public bool InitializedRotation;
}
