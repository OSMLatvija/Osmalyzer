using System;

namespace Osmalyzer;

public class BankPoint : ICorrelatorItem
{
    public BankPointType Type { get; set; }

    public string Name { get; set; }

    public string Address { get; set; }

    public OsmCoord Coord { get; set; }

    public int? Id { get; set; }


    public string TypeString => Type switch
    {
        BankPointType.Branch                  => "Branch",
        BankPointType.AtmWithdrawal           => "ATM",
        BankPointType.AtmWithdrawalAndDeposit => "ATM",
        _                                     => throw new NotImplementedException()
    };


    public BankPoint(BankPointType type, string name, string address, OsmCoord coord)
    {
        Type = type;
        Name = name;
        Address = address;
        Coord = coord;
    }
    
    
    public string ReportString()
    {
        return "Swedbank " + TypeString + " `" + Name + "` " + (Id != null ? "#" + Id + " " : "") + " (`" + Address + "`)";
    }
}

    
public enum BankPointType
{
    Branch,
    AtmWithdrawalAndDeposit,
    AtmWithdrawal
}