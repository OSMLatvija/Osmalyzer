namespace Osmalyzer;

public class ShopData
{
    public string Address { get; }
    public OsmCoord Coord { get; }

            
    public ShopData(string address, OsmCoord coord)
    {
        Address = address;
        Coord = coord;
    }
}