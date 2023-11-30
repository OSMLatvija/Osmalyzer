namespace Osmalyzer;

/// <summary>
/// This will include all OSM elements not matched to anything.
/// Unless all elements given to the correlator are expected on the map, this should be avoided;
/// instead use <see cref="LoneElementAllowanceCallbackParameter"/> to match elements that don't have a data item.
/// </summary>
public class UnmatchedOsmBatch : CorrelatorBatch
{
}