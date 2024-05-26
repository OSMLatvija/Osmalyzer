namespace Osmalyzer;

public class DepositPoint : IDataItem
{
    public OsmCoord Coord { get; }
    
    public string Address { get; }

    public string ShopName { get; }

    public DepositPointMode Mode { get; set; }

    public string DioId { get; }

    public DepositPoint(string dioId, string address, string shopName, DepositPointMode mode, OsmCoord coord)
    {
        DioId = dioId;
        Address = address;
        ShopName = shopName;
        Mode = mode;
        Coord = coord;
    }

    public DepositPoint(DepositPoint point)
    {
        DioId = point.DioId;
        Address = point.Address;
        ShopName = point.ShopName;
        Mode = point.Mode;
        Coord = point.Coord;
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

    // It is here, just to differentiate at report level
    public class DepositAutomat : DepositPoint
    {
        public DepositAutomat(DepositPoint point) : base(point)
        {
        }
    }
}