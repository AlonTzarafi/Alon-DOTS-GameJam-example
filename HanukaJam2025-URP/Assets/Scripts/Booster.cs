using Unity.Entities;
using Unity.Mathematics;

public struct Booster : IComponentData
{
    // public const float FART_RATE = 0.08f;
    public const float FART_RATE = 0.32f;
    public const float FART_TIME_LIMIT = 1.0f;

    public float3 SquadronOriginalCenter;
    public float3 Target;
    public float SpeedToSquadronOriginalCenter;
    public float Speed;

    public float TimeElapsed;
    public float TimeUntilFart;
}
