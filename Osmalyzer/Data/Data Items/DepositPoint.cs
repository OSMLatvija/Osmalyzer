namespace Osmalyzer;

public abstract class DepositPoint : IDataItem
{
    public abstract string TypeString { get; }

    public OsmCoord Coord { get; }

    public string Name => throw new NotSupportedException();
    
    public string Address { get; }

    public string? ShopName { get; }

    public string DioId { get; }

    
    protected DepositPoint(string dioId, string address, string? shopName, OsmCoord coord)
    {
        DioId = dioId;
        Address = address;
        ShopName = shopName;
        Coord = coord;
    }


    public virtual string ReportString()
    {
        return TypeString +
               " (`" + DioId + "`) " +
               (ShopName != null ? " in shop `" + ShopName + "` " : "") +
               "at (`" + Address + "`)";
    }

}

public enum TaromatMode
{
    /// <summary> Accepts items one at a time </summary>
    Taromat,
    /// <summary> Accepts a large number of items at once "poured" in </summary>
    BeramTaromat
}

/// <summary>
/// "Taromāts"
/// </summary>
public class VendingMachineDepositPoint : DepositPoint
{
    public override string TypeString => ModeToString(Mode);

    public TaromatMode Mode { get; }

    
    public VendingMachineDepositPoint(DepositPoint point, TaromatMode mode)
        : base(point.DioId, point.Address, point.ShopName, new OsmCoord(point.Coord.lat, point.Coord.lon))
    {
        Mode = mode;
    }

    
    [Pure]
    private static string ModeToString(TaromatMode mode)
    {
        return mode switch
        {
            TaromatMode.Taromat      => "Taromāts",
            TaromatMode.BeramTaromat => "Beramtaromāts",

            _ => throw new NotImplementedException()
        };
    }
}


public class KioskDepositPoint : DepositPoint
{
    public override string TypeString => "Kiosk";

    
    public KioskDepositPoint(string dioId, string address, string? shopName, OsmCoord coord)
        : base(dioId, address, shopName, coord)
    {
    }
}


public class ManualDepositPoint : DepositPoint
{
    public override string TypeString => "Manual location";
    
    
    public ManualDepositPoint(string dioId, string address, string? shopName, OsmCoord coord)
        : base(dioId, address, shopName, coord)
    {
    }
}

