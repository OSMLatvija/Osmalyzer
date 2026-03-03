namespace Osmalyzer;

/// <summary>
/// Reports OSM elements that were explicitly ignored because <see cref="LoneElementAllowanceParameter"/> returned false for them.
/// These are elements that were not matched to any data item and were not considered "lone" interesting elements,
/// i.e. they appear to be some other kind of element that the correlator should not care about.
/// Only meaningful when a <see cref="LoneElementAllowanceParameter"/> is also provided.
/// </summary>
public class IgnoredOsmBatch : CorrelatorBatch
{
}

