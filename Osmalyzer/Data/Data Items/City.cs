using WikidataSharp;

namespace Osmalyzer;

public class City : IDataItem, IHasWikidataItem, IHasVdbEntry, IHasCspPopulationEntry
{
    public bool Valid { get; }
    
    public string AddressID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string RawAddress { get; }
    
    public string? MunicipalityName { get; }
    
    public OsmMultiPolygon? Boundary { get; }

    public WikidataItem? WikidataItem { get; set; }
    
    public VdbEntry? VdbEntry { get; set; }
    
    public CityStatus? Status { get; set; }
    
    public bool IndependentStateCity { get; set; }
    
    public bool? IsLAUDivision { get; set; }

    public CspPopulationEntry? CspPopulationEntry { get; set; }
    

    public City(bool valid, string addressID, OsmCoord coord, string name, string rawAddress, string? municipalityName, OsmMultiPolygon? boundary)
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
            (Status != null ? Status == CityStatus.StateCity ? "State city" : "Regional city" : "City") +
            " `" + Name + "`" +
            " #`" + AddressID + "`" + 
            (MunicipalityName != null ? " (`" + MunicipalityName + "`)" : "");
    }

    public override string ToString() => ReportString();
}


public enum CityStatus
{
    StateCity,
    RegionalCity
}