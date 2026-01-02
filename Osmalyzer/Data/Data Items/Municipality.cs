using WikidataSharp;

namespace Osmalyzer;

public class Municipality : IDataItem
{
    public bool Valid { get; }
    
    public string AddressID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string RawAddress { get; }
    
    public OsmMultiPolygon? Boundary { get; }

    public WikidataItem? WikidataItem { get; set; }


    public Municipality(bool valid, string addressID, OsmCoord coord, string name, string rawAddress, OsmMultiPolygon? boundary)
    {
        Valid = valid;
        AddressID = addressID;
        Coord = coord;
        Name = name;
        RawAddress = rawAddress;
        Boundary = boundary;
    }
    
    
    public string ReportString()
    {
        return 
            (!Valid ? "Invalid " : "") + 
            "Municipality" + 
            " `" + Name + "`" +
            " #`" + AddressID + "`" + 
            " (`" + RawAddress + "`)";
    }

    public override string ToString() => ReportString();
}