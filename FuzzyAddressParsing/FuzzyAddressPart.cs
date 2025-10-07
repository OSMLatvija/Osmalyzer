namespace Osmalyzer;

public abstract record FuzzyAddressPart(int Index, FuzzyConfidence Confidence)
{
    public List<FuzzyAddressPart>? Siblings { get; private set; }
    
    // Fallback alternatives for ambiguous cases (e.g., house name vs. street+number with equal confidence)
    public List<FuzzyAddressPart>? Fallbacks { get; private set; }
    
    
    public void AddSibling(FuzzyAddressPart sibling)
    {
        if (Siblings == null)
            Siblings = [ sibling ];
        else
            Siblings.Add(sibling);
    }

    public void AddFallback(FuzzyAddressPart fallback)
    {
        if (Fallbacks == null)
            Fallbacks = [ fallback ];
        else
            Fallbacks.Add(fallback);
    }

    
    public abstract string GetQuickString();
}

public record FuzzyAddressStreetNameAndNumberPart(string StreetValue, string NumberValue, string? UnitValue, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string GetQuickString() => "Street `" + StreetValue + "`, Number `" + NumberValue + "`" + (UnitValue != null ? ", Unit `" + UnitValue + "`" : "");
}

public record FuzzyAddressHouseNamePart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string GetQuickString() => "House name `" + Value + "`";
}

public record FuzzyAddressMunicipalityPart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string GetQuickString() => "Municipality `" + Value + "`";
}

public record FuzzyAddressParishPart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string GetQuickString() => "Parish `" + Value + "`";
}

public record FuzzyAddressCityPart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string GetQuickString() => "City `" + Value + "`";
}

public record FuzzyAddressPostcodePart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence)
{
    public override string GetQuickString() => "Postcode `" + Value + "`";
}
