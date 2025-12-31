namespace Osmalyzer;

public class Parish : IDataItem
{
    public bool Valid { get; }
    
    public string ID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string RawAddress { get; }
    
    public string MunicipalityName { get; }
    
    public OsmMultiPolygon? Boundary { get; }


    public Parish(bool valid, string id, OsmCoord coord, string name, string rawAddress, string municipalityName, OsmMultiPolygon? boundary)
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
            "Parish" + 
            " `" + Name + "`" +
            " #`" + ID + "`" + 
            " (`" + MunicipalityName + "`)";
    }
}