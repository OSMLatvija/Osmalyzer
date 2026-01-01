namespace Osmalyzer;

public abstract record SuggestedAction(OsmElement Element);

public record OsmSetValueSuggestedAction(OsmElement Element, string Key, string Value) : SuggestedAction(Element);