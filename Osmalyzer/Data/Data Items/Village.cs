using WikidataSharp;

namespace Osmalyzer;

public class Village : IDataItem
{
    public bool Valid { get; }
    
    public string ID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string RawAddress { get; }
    
    public string ParishName { get; }
    
    public string MunicipalityName { get; }
    
    public OsmMultiPolygon? Boundary { get; }

    public WikidataItem? WikidataItem { get; set; }


    public Village(bool valid, string id, OsmCoord coord, string name, string rawAddress, string parishName, string municipalityName, OsmMultiPolygon? boundary)
    {
        Valid = valid;
        ID = id;
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
            " #`" + ID + "`" + 
            " (`" + ParishName + ", " + MunicipalityName + "`)";
    }
}