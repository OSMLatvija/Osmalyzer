namespace Osmalyzer;

public abstract record FuzzyAddressPart(int Index, FuzzyConfidence Confidence)
{
    public List<FuzzyAddressPart>? Siblings { get; private set; }
    
    
    public void AddSibling(FuzzyAddressPart sibling)
    {
        if (Siblings == null)
            Siblings = [ sibling ];
        else
            Siblings.Add(sibling);
    }

    
    public abstract string? GetQuickString();
}

public record FuzzyAddressStreetNameAndNumberPart(string StreetValue, string NumberValue, string? UnitValue, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string? GetQuickString() => "Street `" + StreetValue + "`, Number `" + NumberValue + "`" + (UnitValue != null ? ", Unit `" + UnitValue + "`" : "");
}

public record FuzzyAddressHouseNamePart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string? GetQuickString() => "House name `" + Value + "`";
}

public record FuzzyAddressMunicipalityPart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string? GetQuickString() => "Municipality `" + Value + "`";
}

public record FuzzyAddressParishPart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string? GetQuickString() => "Parish `" + Value + "`";
}

public record FuzzyAddressCityPart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string? GetQuickString() => "City `" + Value + "`";
}

public record FuzzyAddressPostcodePart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string? GetQuickString() => "Postcode `" + Value + "`";
}
