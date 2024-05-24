namespace Osmalyzer;

public class DepositPoint : IDataItem
{
    public OsmCoord Coord { get; }
    
    public string Address { get; }

    public string ShopName { get; }

    public DepositPointMode Mode { get; }

    public string DioId { get; }

    public DepositPoint(string dioId, string address, string shopName, DepositPointMode mode, OsmCoord coord)
    {
        DioId = dioId;
        Address = address;
        ShopName = shopName;
        Mode = mode;
        Coord = coord;
    }

    public string ReportString()
    {
        return Mode.ToString() + " Deposit point " + DioId +
           (ShopName != null ? " in shop '" + ShopName + "'" : "") + 
           " (`" + Address + "`)";
    }

    public enum DepositPointMode
    {
        Automatic,
        Manual,
        BeramTaromats
    }
}