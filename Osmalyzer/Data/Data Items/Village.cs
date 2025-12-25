namespace Osmalyzer;

public class Village : IDataItem
{
    public bool Valid { get; }
    
    public string ID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string Address { get; }
    
    public bool IsHamlet { get; }


    public Village(bool valid, string id, OsmCoord coord, string name, string address, bool isHamlet)
    {
        Valid = valid;
        ID = id;
        Coord = coord;
        Name = name;
        Address = address;
        IsHamlet = isHamlet;
    }
    
    
    public string ReportString()
    {
        return 
            (!Valid ? "Invalid " : "") + 
            (IsHamlet ? "Hamlet" : "Village") + 
            " `" + Name + "`" +
            " #`" + ID + "`" + 
            " (`" + Address + "`)";
    }
}