using System;
using Unity.Entities;

public struct Quadrant : IComponentData
{
    public QuadrantUpdateMode UpdateMode;

    public int Index;

    public static bool IsInitialized(QuadrantUpdateMode updateMode)
    {
        var initializedStates = (byte)QuadrantUpdateMode.OneTimeInitialized | (byte)QuadrantUpdateMode.ContinuousInitialized;
        var match = ((byte)updateMode & initializedStates) != 0;
        return match;

    }

    public static bool ShouldUpdate(QuadrantUpdateMode updateMode)
    {
        var dontUpdate = (byte)QuadrantUpdateMode.Unused | (byte)QuadrantUpdateMode.OneTimeInitialized;
        var match = ((byte)updateMode & dontUpdate) != 0;
        return !match;
    }

    public static QuadrantUpdateMode AfterUpdate(QuadrantUpdateMode updateMode)
    {
        if (updateMode == QuadrantUpdateMode.OneTimeNotInitialized) {
            return QuadrantUpdateMode.OneTimeInitialized;
        }
        if (updateMode == QuadrantUpdateMode.ContinuousNotInitialized) {
            return QuadrantUpdateMode.ContinuousInitialized;
        }
        return QuadrantUpdateMode.Unused;
    }
}

public enum QuadrantUpdateMode : byte
{
    Unused = 0,
    OneTimeNotInitialized = 1,
    OneTimeInitialized = 2,
    ContinuousNotInitialized = 3,
    ContinuousInitialized = 4,
}

public enum QuadrantUpdateModeForAuthoring : byte
{
    Unused = QuadrantUpdateMode.Unused,
    OneTime = QuadrantUpdateMode.OneTimeNotInitialized,
    Continuous = QuadrantUpdateMode.ContinuousNotInitialized,
}
