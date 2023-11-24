using JetBrains.Annotations;

namespace Osmalyzer;

/// <summary>
/// The non-OSM data item
/// </summary>
public interface ICorrelatorItem
{
    public OsmCoord Coord { get; }


    [Pure]
    public string ReportString();
}