using WikidataSharp;

namespace Osmalyzer;

public class City : IDataItem
{
    public bool Valid { get; }
    
    public string ID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string RawAddress { get; }
    
    public string? MunicipalityName { get; }
    
    public OsmMultiPolygon? Boundary { get; }

    public WikidataItem? WikidataItem { get; set; }
    
    public CityStatus? Status { get; set; }
    
    public bool? IsLAUDivision { get; set; }


    public City(bool valid, string id, OsmCoord coord, string name, string rawAddress, string? municipalityName, OsmMultiPolygon? boundary)
    {
        Valid = valid;
        ID = id;
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
            (Status != null ? Status == CityStatus.StateCity ? "State city" : "Regional city" : "City") +
            " `" + Name + "`" +
            " #`" + ID + "`" + 
            (MunicipalityName != null ? " (`" + MunicipalityName + "`)" : "");
    }
}


public enum CityStatus
{
    StateCity,
    RegionalCity
}