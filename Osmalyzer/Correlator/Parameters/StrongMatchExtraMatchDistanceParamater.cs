namespace Osmalyzer;

/// <summary>
/// In addition to <see cref="MatchDistanceParamater"/> and <see cref="MatchFarDistanceParamater"/>,
/// extend the distance at which items can be matched to OSM elements
/// when the items match to OSM elements as a <see cref="MatchStrength.Strong"/> match,
/// determined with <see cref="MatchCallbackParameter{T}"/> via whatever custom rules may be appropriate.
/// This is when we know data has skewed coordinates, but is most likely referring to the OSM elements, such as using a matching address or ref number.
/// </summary>
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