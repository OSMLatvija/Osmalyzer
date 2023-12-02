namespace Osmalyzer;

public class CourthouseData : IDataItem
{
    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string Address { get; }


    public CourthouseData(OsmCoord coord, string name, string address)
    {
        Coord = coord;
        Name = name;
        Address = address;
    }


    public string ReportString()
    {
        return "Courthouse `" + Name + "` (`" + Address + "`)";
    }
}