namespace Osmalyzer;

/// <summary>
/// This will include all OSM elements not matched to anything.
/// If all elements given to the correlator are not necessarilly expected on the map,
/// instead use <see cref="LoneElementAllowanceParameter"/> to match elements that appear to be something we should be matching
/// but don't have a data item and use <see cref="MatchedLoneOsmBatch"/> to list them.
/// </summary>
public class UnmatchedOsmBatch : CorrelatorBatch
{
}