using WikidataSharp;

namespace Osmalyzer;

public class Parish : IDataItem, IHasWikidataItem
{
    public bool Valid { get; }
    
    public string AddressID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string RawAddress { get; }
    
    public string MunicipalityName { get; }
    
    public OsmMultiPolygon? Boundary { get; }

    public WikidataItem? WikidataItem { get; set; }


    public Parish(bool valid, string addressID, OsmCoord coord, string name, string rawAddress, string municipalityName, OsmMultiPolygon? boundary)
    {
        Valid = valid;
        AddressID = addressID;
        Coord = coord;
        Name = name;
        RawAddress = rawAddress;
        MunicipalityName = municipalityName;
        Boundary = boundary;
    }
    
    
    public string ReportString()
    {
        return 
            (!Valid ? "Invalid " : "") + 
            "Parish" + 
            " `" + Name + "`" +
            " #`" + AddressID + "`" + 
            " (`" + MunicipalityName + "`)";
    }

    public override string ToString() => ReportString();
}