namespace Osmalyzer;

public class CulturalCenterData : IDataItem
{
    public string Name { get; }

    public string Address { get; }

    public OsmCoord Coord { get; }


    public CulturalCenterData(string name, string address, OsmCoord coord)
    {
        Name = name;
        Address = address;
        Coord = coord;
    }


    [Pure]
    public string ReportString()
    {
        return "Cultural center `" + Name + "` at `" + Address + "`";
    }
}
