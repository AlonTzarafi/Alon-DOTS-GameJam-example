using Unity.Entities;
using Unity.Mathematics;

public struct CameraData : IComponentData
{
    public float4x4 ViewProjectionMatrix;

    public float2 ScreenSize;
}
