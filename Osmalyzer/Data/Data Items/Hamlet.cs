using WikidataSharp;

namespace Osmalyzer;

public class Hamlet : IDataItem
{
    public bool Valid { get; }
    
    public string ID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string RawAddress { get; }
    
    public string ParishName { get; }
    
    public string MunicipalityName { get; }

    public WikidataItem? WikidataItem { get; set; }


    public Hamlet(bool valid, string id, OsmCoord coord, string name, string rawAddress, string parishName, string municipalityName)
    {
        Valid = valid;
        ID = id;
        Coord = coord;
        Name = name;
        RawAddress = rawAddress;
        ParishName = parishName;
        MunicipalityName = municipalityName;
    }
    
    
    public string ReportString()
    {
        return 
            (!Valid ? "Invalid " : "") + 
            "Hamlet `" + Name + "`" +
            " #`" + ID + "`" + 
            " (`" + ParishName + ", " + MunicipalityName + "`)";
    }
}

