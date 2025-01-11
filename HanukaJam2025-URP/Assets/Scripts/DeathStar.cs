using Unity.Entities;
using Unity.Mathematics;

public struct DeathStar : IComponentData
{
    public float Shield;
    public float Health;

    public bool Died;

    public int ShieldHits;

    public float GetDamage()
    {
        return 1f - Health;
    }
}
