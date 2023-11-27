namespace Osmalyzer;

public class CulturalMonument : ICorrelatorItem
{
    public OsmCoord Coord { get; }

    public string Name { get; }
    
    public int? ReferenceID { get; }


    public CulturalMonument(OsmCoord coord, string name, int? referenceId)
    {
        Coord = coord;
        Name = name;
        ReferenceID = referenceId;
    }

    
    public string ReportString()
    {
        return "#" + (ReferenceID?.ToString() ?? "?") + " \"" + Name + "\" " + Coord.OsmUrl;
    }
}