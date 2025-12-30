namespace Osmalyzer;

public class Parish : IDataItem
{
    public bool Valid { get; }
    
    public string ID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string Address { get; }
    
    public OsmMultiPolygon? Boundary { get; }


    public Parish(bool valid, string id, OsmCoord coord, string name, string address, OsmMultiPolygon? boundary)
    {
        Valid = valid;
        ID = id;
        Coord = coord;
        Name = name;
        Address = address;
        Boundary = boundary;
    }
    
    
    public string ReportString()
    {
        return 
            (!Valid ? "Invalid " : "") + 
            "Parish" + 
            " `" + Name + "`" +
            " #`" + ID + "`" + 
            " (`" + Address + "`)";
    }
}