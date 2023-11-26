using System;

namespace Osmalyzer;

/// <summary>
/// Shown on a map
/// </summary>
public class MapPointReportEntry : ReportEntry
{
    public OsmCoord Coord { get; }
    
    public MapPointStyle Style { get; set; }


    private int _pointCount = 1;


    public MapPointReportEntry(OsmCoord coord, string text, MapPointStyle style, ReportEntryContext? context = null)
        : base(text, context)
    {
        Coord = coord;
        Style = style;
    }

        
    public void Append(MapPointReportEntry otherPoint)
    {
        if (_pointCount == 1)
            Text = "#1: " + Text;
                
        Text += Environment.NewLine + "#" + (_pointCount + 1) + ": " + otherPoint.Text;

        _pointCount++;
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