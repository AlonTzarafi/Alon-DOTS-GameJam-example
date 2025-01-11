using Unity.Entities;
using Unity.Mathematics;

public struct ToBeDestroyed : ICleanupComponentData
{
    public int CyclesElapsed;
}
