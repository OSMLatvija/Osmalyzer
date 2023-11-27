namespace Osmalyzer;

public class CulturalMonument : ICorrelatorItem
{
    public OsmCoord Coord { get; }

    public string Name { get; }


    public CulturalMonument(OsmCoord coord, string name)
    {
        Coord = coord;
        Name = name;
    }

    
    public string ReportString()
    {
        return Coord.OsmUrl;
    }
}