namespace Osmalyzer;

public class Village : IDataItem
{
    public bool Valid { get; }
    
    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string Address { get; }
    
    public bool IsHamlet { get; }


    public Village(bool valid, OsmCoord coord, string name, string address, bool isHamlet)
    {
        Valid = valid;
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
            " `" + Name + 
            "` (`" + Address + "`)";
    }
}