using System;

namespace Osmalyzer;

/// <summary>
/// Shown on a map
/// </summary>
public class MapPointReportEntry : ReportEntry
{
    public OsmCoord Coord { get; }


    private int _pointCount = 1;


    public MapPointReportEntry(OsmCoord coord, string text, ReportEntryContext? context = null)
        : base(text, context)
    {
        Coord = coord;
    }

        
    public void Append(MapPointReportEntry otherPoint)
    {
        if (_pointCount == 1)
            Text = "<b>#1</b> - " + Text;
                
        Text += "<b>#" + (_pointCount + 1) + "</b> - " + Environment.NewLine + otherPoint;

        _pointCount++;
    }
}