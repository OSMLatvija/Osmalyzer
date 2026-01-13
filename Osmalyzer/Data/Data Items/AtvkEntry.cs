using WikidataSharp;

namespace Osmalyzer;

public class AtvkEntry : IDataItem, IHasCspPopulationEntry, IHasWikidataItem, IHasVdbEntry, IHasAtvkEntry
{
    /// <summary> NUTS or LAU code </summary>
    public string Code { get; }
    
    public string Name { get; }
    
    public AtvkLevel Level { get; }

    public AtvkDesignation Designation { get; }

    public string? CodeParent { get; }
    
    public DateTime ValidityBegin { get; }
    
    public DateTime? ValidityEnd { get; }

    public AtvkEntry? Parent { get; internal set; }
    
    public List<AtvkEntry>? Children { get; internal set; }

    public CspPopulationEntry? CspPopulationEntry { get; set; }

    public WikidataItem WikidataItem
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public VdbEntry VdbEntry
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    AtvkEntry IHasAtvkEntry.AtvkEntry
    {
        get => throw new InvalidOperationException();
        set => throw new NotImplementedException();
    }


    public bool IsExpired => ValidityEnd != null;

    public OsmCoord Coord => throw new NotSupportedException("ATVK entries do not have coordinates.");
    

    public AtvkEntry(string code, string name, AtvkLevel level, AtvkDesignation designation, string? codeParent, DateTime validityBegin, DateTime? validityEnd)
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
            AtvkDesignation.Country            => "Country",
            AtvkDesignation.Region             => "Region",
            AtvkDesignation.CityInRegion       => "City (in region)",
            AtvkDesignation.Municipality       => "Municipality",
            AtvkDesignation.CityInMunicipality => "City (in municipality)",
            AtvkDesignation.Parish             => "Parish",
            _                                  => throw new Exception()
        };
    }

    public override string ToString() => ReportString();
}

public enum AtvkLevel
{
    Country,
    Region,
    CityOrMunicipality,
    CityOrParish,
    Expired
}

public enum AtvkDesignation
{
    Country,
    Region,
    CityInRegion,
    Municipality,
    CityInMunicipality,
    Parish,
    Expired
}