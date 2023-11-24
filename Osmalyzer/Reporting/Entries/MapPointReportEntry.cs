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
            Text = "#1: " + Text;
                
        Text += Environment.NewLine + "#" + (_pointCount + 1) + ": " + otherPoint.Text;

        _pointCount++;
    }
}