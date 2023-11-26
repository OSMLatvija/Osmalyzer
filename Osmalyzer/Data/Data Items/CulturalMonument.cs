namespace Osmalyzer;

public class CulturalMonument : ICorrelatorItem
{
    public OsmCoord Coord { get; }

    
    public CulturalMonument(OsmCoord coord)
    {
        Coord = coord;
    }

    
    public string ReportString()
    {
        return Coord.OsmUrl;
    }
}