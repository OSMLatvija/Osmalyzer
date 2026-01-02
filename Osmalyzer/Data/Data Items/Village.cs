using WikidataSharp;

namespace Osmalyzer;

public class Village : IDataItem
{
    public bool Valid { get; }
    
    public string AddressID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string RawAddress { get; }
    
    public string ParishName { get; }
    
    public string MunicipalityName { get; }
    
    public OsmMultiPolygon? Boundary { get; }

    public WikidataItem? WikidataItem { get; set; }


    public Village(bool valid, string addressID, OsmCoord coord, string name, string rawAddress, string parishName, string municipalityName, OsmMultiPolygon? boundary)
    {
        Valid = valid;
        AddressID = addressID;
        Coord = coord;
        Name = name;
        RawAddress = rawAddress;
        ParishName = parishName;
        MunicipalityName = municipalityName;
        Boundary = boundary;
    }
    
    
    public string ReportString()
    {
        return 
            (!Valid ? "Invalid " : "") + 
            "Village `" + Name + "`" +
            " #`" + AddressID + "`" + 
            " (`" + ParishName + ", " + MunicipalityName + "`)";
    }

    public override string ToString() => ReportString();
}