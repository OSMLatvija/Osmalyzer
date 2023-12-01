namespace Osmalyzer;

public class ShopData : IDataItem
{
    public string ShopName { get; }

    public string Address { get; }
    
    public OsmCoord Coord { get; }


    public ShopData(string shopName, string address, OsmCoord coord)
    {
        Address = address;
        Coord = coord;
        ShopName = shopName;
    }
    
    
    public string ReportString()
    {
        return
            ShopName + " shop " +
            "`" + Address + "`";
    }
}