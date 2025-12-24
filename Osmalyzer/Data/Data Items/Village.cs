namespace Osmalyzer;

public class Village : IDataItem
{
    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string Address { get; }


    public Village(OsmCoord coord, string name, string address)
    {
        Coord = coord;
        Name = name;
        Address = address;
    }
    
    
    public string ReportString()
    {
        return "Village `" + Name + "` (`" + Address + "`)";
    }
}