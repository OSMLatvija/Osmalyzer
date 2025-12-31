namespace Osmalyzer;

public abstract record SuggestedChange;

public record AddValueSuggested(OsmElement Element, string Key, string Value) : SuggestedChange;