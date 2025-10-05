namespace Osmalyzer;

public abstract record FuzzyAddressPart(int Index, FuzzyConfidence Confidence)
{
    public FuzzyAddressPart? Sibling { get; private set; }
    
    
    public void SetSibling(FuzzyAddressPart sibling)
    {
        Sibling = sibling;
    }
}

public record FuzzyAddressStreetNameAndNumberPart(string StreetValue, string NumberValue, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence);

public record FuzzyAddressHouseNamePart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence);

public record FuzzyAddressCityPart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence);

public record FuzzyAddressPostcodePart(string Value, int Index, FuzzyConfidence Confidence) : FuzzyAddressPart(Index, Confidence);
