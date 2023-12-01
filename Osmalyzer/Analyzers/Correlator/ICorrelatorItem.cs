using JetBrains.Annotations;

namespace Osmalyzer;

/// <summary>
/// The non-OSM data item
/// </summary>
public interface ICorrelatorItem
{
    public OsmCoord Coord { get; }


    /// <summary>
    /// User-readable unique description of this item, such as its key values.
    /// This should include the previx label describing what sort of item this is - reports won't assume best label.
    /// This shouldn't include the <see cref="Coord"/>, as that will be reported as needed depending on the actual issue. 
    /// </summary>
    [Pure]
    public string ReportString();
}