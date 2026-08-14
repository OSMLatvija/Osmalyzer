namespace Osmalyzer;

/// <summary>
/// Allows an OSM element to match to multiple data items.
/// This will check already-matched OSM elements against whether it can match repeats and then use the match callback to match again.
/// This is an exceptional case and not usually desired.
/// But sometimes "reality" has one element mapped for something that data represents as multiple.
/// This is difficult to properly "link" to data items, but technically possible with more rules.
/// For example, GTFS data might have 2 stops for a single physical platform stop location.
/// This adds an additional check for elements that explicitly could be multi-matches.
/// For example, having multiple ref values.
/// </summary>
public class MatchRepeatedParameter : CorrelatorParamater
{
    public Func<OsmElement, bool> CanOsmElementMatchMultipleCallback { get; }
    
    public MatchStrength MinMatchStrength { get; }


    public MatchRepeatedParameter(Func<OsmElement, bool> canOsmElementMatchMultipleCallback, MatchStrength minMatchStrength = MatchStrength.Strong)
    {
        CanOsmElementMatchMultipleCallback = canOsmElementMatchMultipleCallback;
        MinMatchStrength = minMatchStrength;
    }
}