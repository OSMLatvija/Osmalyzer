namespace Osmalyzer;

public class Hamlet : IDataItem
{
    public bool Valid { get; }
    
    public string ID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string Address { get; }


    public Hamlet(bool valid, string id, OsmCoord coord, string name, string address)
    {
        Valid = valid;
        ID = id;
        Coord = coord;
        Name = name;
        Address = address;
    }
    
    
    public string ReportString()
    {
        return 
            (!Valid ? "Invalid " : "") + 
            "Hamlet `" + Name + "`" +
            " #`" + ID + "`" + 
            " (`" + Address + "`)";
    }
}

