using System;

namespace Osmalyzer;

public abstract class BankPoint : ICorrelatorItem
{
    public string Name { get; set; }

    public string Address { get; set; }

    public OsmCoord Coord { get; set; }

    public int? Id { get; set; }


    public abstract string TypeString { get; }


    protected BankPoint(string name, string address, OsmCoord coord)
    {
        Name = name;
        Address = address;
        Coord = coord;
    }
    
    
    public string ReportString()
    {
        return "Swedbank " + TypeString + " `" + Name + "` " + (Id != null ? "#" + Id + " " : "") + " (`" + Address + "`)";
    }
}

public class BankAtmPoint : BankPoint
{
    public override string TypeString => "ATM";
    
    public bool? Deposit { get; set; }

    
    public BankAtmPoint(string name, string address, OsmCoord coord, bool? deposit)
        : base(name, address, coord)
    {
        Deposit = deposit;
    }
}

public class BankBranchPoint : BankPoint
{
    public override string TypeString => "branch";
    
    
    public BankBranchPoint(string name, string address, OsmCoord coord)
        : base(name, address, coord)
    {
    }
}

