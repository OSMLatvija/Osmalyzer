namespace Osmalyzer;

public abstract record FuzzyAddressHint(int Index);


public record FuzzyAddressStreetLineHint(int Index) : FuzzyAddressHint(Index);

public record FuzzyAddressCityHint(int Index) : FuzzyAddressHint(Index);

public record FuzzyAddressPostcodeHint(int Index) : FuzzyAddressHint(Index);