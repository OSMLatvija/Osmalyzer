namespace Osmalyzer;

public class ShopData : ICorrelatorItem
{
    public string Address { get; }
    
    public OsmCoord Coord { get; }


    public ShopData(string address, OsmCoord coord)
    {
        Address = address;
        Coord = coord;
    }
    
    
    public string ReportString()
    {
        return
            "\"" + Address + "\" " +
            "found around " + Coord.OsmUrl;
    }
}