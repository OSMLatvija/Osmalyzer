using JetBrains.Annotations;

namespace Osmalyzer;

public abstract class BankPoint : ICorrelatorItem
{
    public string BankName { get; set; }
    
    public string Name { get; set; }

    public string? Address { get; set; }

    public OsmCoord Coord { get; set; }

    public int? DisambiguationId { get; set; }


    public abstract string TypeString { get; }


    protected BankPoint(string bankName, string name, string? address, OsmCoord coord)
    {
        BankName = bankName;
        Name = name;
        Address = address;
        Coord = coord;
    }
    
    
    [Pure]
    public string ReportString()
    {
        return 
            BankName + 
            " " + TypeString + 
            " `" + Name + "`" + 
            (DisambiguationId != null ? " #" + DisambiguationId + " " : "") + 
            (Address != null ? " (`" + Address + "`)" : "");
    }
}

public class BankAtmPoint : BankPoint
{
    public override string TypeString => "ATM";
    
    
    public bool? Deposit { get; set; }

    
    public BankAtmPoint(string bankName, string name, string? address, OsmCoord coord, bool? deposit)
        : base(bankName, name, address, coord)
    {
        Deposit = deposit;
    }
}

public class BankBranchPoint : BankPoint
{
    public override string TypeString => "branch";
    
    
    public BankBranchPoint(string bankName, string name, string? address, OsmCoord coord)
        : base(bankName, name, address, coord)
    {
    }
}

