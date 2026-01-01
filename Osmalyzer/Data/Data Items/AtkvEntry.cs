namespace Osmalyzer;

public class AtkvEntry : IDataItem
{
    public string Code { get; }
    
    public string Name { get; }
    
    public AtkvLevel Level { get; }

    public AtkvDesignation Designation { get; }

    public string? CodeParent { get; }
    
    public DateTime ValidityBegin { get; }
    
    public DateTime? ValidityEnd { get; }

    public AtkvEntry? Parent { get; internal set; }

    
    public bool IsExpired => ValidityEnd != null;

    public OsmCoord Coord => throw new NotSupportedException("ATVK entries do not have coordinates.");
    

    public AtkvEntry(string code, string name, AtkvLevel level, AtkvDesignation designation, string? codeParent, DateTime validityBegin, DateTime? validityEnd)
    {
        Code = code;
        Name = name;
        Level = level;
        Designation = designation;
        CodeParent = codeParent;
        ValidityBegin = validityBegin;
        ValidityEnd = validityEnd;
    }


    public string ReportString()
    {
        return
            (IsExpired ? "[Expired] Unit L-" + Level : LevelName()) +
            " `" + Name + "`" +
            " #`" + Code + "`" +
            (!IsExpired && Parent != null ? " under `" + Parent.Name + "`" : "");


        [Pure]
        string LevelName() => Designation switch
        {
            AtkvDesignation.Country      => "Country",
            AtkvDesignation.Region       => "Region",
            AtkvDesignation.StateCity    => "State city",
            AtkvDesignation.Municipality => "Municipality",
            AtkvDesignation.RegionalCity => "Regional city",
            AtkvDesignation.Parish       => "Parish",
            _                            => throw new Exception()
        };
    }
}

public enum AtkvLevel
{
    Country,
    Region,
    StateCityOrMunicipality,
    CityOrParish,
    Expired
}

public enum AtkvDesignation
{
    Country,
    Region,
    StateCity,
    Municipality,
    RegionalCity,
    Parish,
    Expired
}