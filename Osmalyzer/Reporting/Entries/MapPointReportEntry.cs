namespace Osmalyzer;

/// <summary>
/// Shown on a map
/// </summary>
public class MapPointReportEntry : ReportEntry
{
    public OsmCoord Coord { get; }
    
    public MapPointStyle Style { get; }


    public MapPointReportEntry(OsmCoord coord, string text, MapPointStyle style, ReportEntryContext? context = null)
        : base(text, context)
    {
        Coord = coord;
        Style = style;
    }
}

public enum MapPointStyle
{
    Okay,
    Problem,
    Dubious,

    CorrelatorPairMatched,
    CorrelatorPairMatchedOffsetOrigin,
    CorrelatorPairMatchedFar,
    CorrelatorPairMatchedFarOrigin,
    CorrelatorItemUnmatched,
    CorrelatorOsmUnmatched,
    CorrelatorLoneOsmMatched,
    CorrelatorLoneOsmUnmatched
}