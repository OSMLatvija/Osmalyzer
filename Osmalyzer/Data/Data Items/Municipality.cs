namespace Osmalyzer;

public class Municipality : IDataItem
{
    public bool Valid { get; }
    
    public string ID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string Address { get; }
    
    public OsmMultiPolygon? Boundary { get; }


    public Municipality(bool valid, string id, OsmCoord coord, string name, string address, OsmMultiPolygon? boundary)
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
            "Municipality" + 
            " `" + Name + "`" +
            " #`" + ID + "`" + 
            " (`" + Address + "`)";
    }
}