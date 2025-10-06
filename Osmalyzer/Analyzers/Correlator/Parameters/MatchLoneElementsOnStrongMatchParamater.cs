namespace Osmalyzer;

/// <summary>
/// If an OSM element is considered allowable by itself via <see cref="LoneElementAllowanceParameter"/>,
/// attempt to match it to any data item at any distance when the <see cref="MatchCallbackParameter{T}"/>
/// reports at least the specified <see cref="Strength"/>.
/// This effectively upgrades certain "lone" elements into matched pairs regardless of distance when
/// they are a strong semantic match.
/// </summary>
public class MatchLoneElementsOnStrongMatchParamater : CorrelatorParamater
{
    public MatchStrength Strength { get; }

    public MatchLoneElementsOnStrongMatchParamater(MatchStrength strength)
    {
        if (strength == MatchStrength.Unmatched) throw new ArgumentOutOfRangeException();
        if (strength == MatchStrength.Regular) throw new InvalidOperationException("Only higher strengths can allow lone matching at any distance, " + nameof(MatchStrength.Regular) + " is default");

        Strength = strength;
    }
}

