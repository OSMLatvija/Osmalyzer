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

    public MapPointReportEntry(OsmCoord coord, string text, List<OsmElement> elements, MapPointStyle style, ReportEntryContext? context = null)
        : this(coord, AddElementTagsToText(text, elements), style, context) { }

    public MapPointReportEntry(OsmCoord coord, string text, OsmElement element, MapPointStyle style, ReportEntryContext? context = null)
        : this(coord, AddElementTagsToText(text, new List<OsmElement> { element }), style, context) { }


    [Pure]
    private static string AddElementTagsToText(string text, List<OsmElement> elements)
    {
        foreach (OsmElement element in elements)
            text += OsmElementTagsBlock(element);

        return text;
        

        [Pure]
        static string? OsmElementTagsBlock(OsmElement osmElement)
        {
            string? tags = osmElement.GetAllTagsAsString();

            return tags != null ? Environment.NewLine + "```" + osmElement.ElementType + " #" + osmElement.Id + Environment.NewLine + tags + "```" : null;
        }
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
    CorrelatorLoneOsmUnmatched,
    
    BusStopMatchedWell,
    BusStopMatchedPoorly,
    BusStopOriginalUnmatched,
    BusStopOsmUnmatched
}