namespace Osmalyzer;

public class Village : IDataItem
{
    public bool Valid { get; }
    
    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string Address { get; }


    public Village(bool valid, OsmCoord coord, string name, string address)
    {
        Valid = valid;
        Coord = coord;
        Name = name;
        Address = address;
    }
    
    
    public string ReportString()
    {
        return (!Valid ? "Invalid " : "") + "Village `" + Name + "` (`" + Address + "`)";
    }
}