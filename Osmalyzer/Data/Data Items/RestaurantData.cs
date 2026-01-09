namespace Osmalyzer;

public class RestaurantData : IDataItem
{
    public string RestaurantName { get; }

    public string Name => RestaurantName;

    public string? Address { get; }

    public OsmCoord Coord { get; }


    public RestaurantData(string restaurantName, string? address, OsmCoord coord)
    {
        Address = address;
        Coord = coord;
        RestaurantName = restaurantName;
    }


    public string ReportString()
    {
        return
            RestaurantName + " restaurant" +
            (Address != null ? " `" + Address + "`" : "");
    }
}