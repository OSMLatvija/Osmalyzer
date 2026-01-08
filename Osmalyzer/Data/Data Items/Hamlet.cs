using WikidataSharp;

namespace Osmalyzer;

public class Hamlet : IDataItem, IHasWikidataItem, IHasVdbEntry
{
    public bool Valid { get; }
    
    public string AddressID { get; }

    public OsmCoord Coord { get; }
    
    public string Name { get; }
    
    public string RawAddress { get; }
    
    public string ParishName { get; }
    
    public string MunicipalityName { get; }

    public WikidataItem? WikidataItem { get; set; }

    public VdbEntry? VdbEntry { get; set; }


    public Hamlet(bool valid, string addressID, OsmCoord coord, string name, string rawAddress, string parishName, string municipalityName)
    {
        Valid = valid;
        AddressID = addressID;
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
            " #`" + AddressID + "`" + 
            " (`" + ParishName + ", " + MunicipalityName + "`)";
    }

    public override string ToString() => ReportString();
}

