using System;

namespace Osmalyzer;

abstract public class DepositPoint : IDataItem
{
    public abstract string TypeString { get; }

    public OsmCoord Coord { get; }
    
    public string Address { get; }

    public string ShopName { get; }

    public string DioId { get; }

    public DepositPoint(string dioId, string address, string shopName, OsmCoord coord)
    {
        DioId = dioId;
        Address = address;
        ShopName = shopName;
        Coord = coord;
    }

    public virtual string ReportString()
    {
        return TypeString + " (" + DioId + ") " + 
           (ShopName != null ? "in shop '" + ShopName + "' " : "") + 
           "at (`" + Address + "`)";
    }

}

public enum TaromatMode
{
    Taromat,
    BeramTaromat
}

public class DepositAutomat : DepositPoint
{
    public override string TypeString => "Taromat";

    public TaromatMode Mode { get; }

    public DepositAutomat(AutomatedDepositLocation point, TaromatMode mode)
        : base(point.DioId, point.Address, point.ShopName, new OsmCoord(point.Coord.lat, point.Coord.lon))
    {
        Mode = mode;
    }

    public override string ReportString()
    {
        return Mode.ToString() + (ShopName != null ? "in shop '" + ShopName + "' " : "") + 
           "at (`" + Address + "`)";
    }
}

public class AutomatedDepositLocation : DepositPoint
{
    public AutomatedDepositLocation(string dioId, string address, string shopName, OsmCoord coord)
        : base(dioId, address, shopName, coord)
    {
    }

    public override string TypeString => "Automated redemption location";
}

public class ManualDepositLocation : DepositPoint
{
    public override string TypeString => "Manual redemption location";
    
    public ManualDepositLocation(string dioId, string address, string shopName, OsmCoord coord)
        : base(dioId, address, shopName, coord)
    {
    }

}

