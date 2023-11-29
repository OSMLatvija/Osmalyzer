using System;

namespace Osmalyzer;

public class MatchExtraDistanceParamater : CorrelatorParamater
{
    public MatchStrength Strength { get; }
    
    public int ExtraDistance { get; }

    
    public MatchExtraDistanceParamater(MatchStrength strength, int extraDistance)
    {
        if (strength == MatchStrength.Unmatched) throw new ArgumentOutOfRangeException();
        if (strength == MatchStrength.Regular) throw new InvalidOperationException("Only higher strengths can add distance, " + nameof(MatchStrength.Regular) + " is default");
        
        Strength = strength;
        ExtraDistance = extraDistance;
    }
}