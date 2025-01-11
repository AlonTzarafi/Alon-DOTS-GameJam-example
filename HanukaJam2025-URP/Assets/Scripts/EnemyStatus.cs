using Unity.Entities;
using Unity.Mathematics;

public struct EnemyStatus : IComponentData
{
    public float TotalHealth;
    public bool IsTooDamagedToFunction;
}
