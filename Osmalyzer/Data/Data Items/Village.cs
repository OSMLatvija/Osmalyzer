namespace Osmalyzer;

public class Village : IDataItem
{
    public OsmCoord Coord { get; }
    
    public string Name { get; }


    public Village(OsmCoord coord, string name)
    {
        Coord = coord;
        Name = name;
    }
    
    
    public string ReportString()
    {
        return "Village `" + Name + "` ";
    }
}