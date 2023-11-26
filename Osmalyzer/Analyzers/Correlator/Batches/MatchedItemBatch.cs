namespace Osmalyzer;

/// <summary>
/// This normally includes data to OSM elements matches, optionally filtered by <see cref="MatchCallbackParameter{T}"/>.
/// This can also include OSM elements by themselves if filtered by <see cref="LoneElementAllowanceCallbackParameter"/>.
/// </summary>
public class MatchedItemBatch : CorrelatorBatch
{
}