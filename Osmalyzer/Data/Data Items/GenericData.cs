namespace Osmalyzer;

public class GenericData : IDataItem
{
    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string Address { get; }

    public string Type { get; }


    public GenericData(OsmCoord coord, string name, string address, string type)
    {
        Coord = coord;
        Name = name;
        Address = address;
        Type = type;
    }


    public string ReportString()
    {
        return Type + " `" + Name + "` (`" + Address + "`)";
    }
}