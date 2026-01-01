namespace Osmalyzer;

public abstract record OsmChangeAction;

public record OsmSetValueAction(OsmElement Element, string Key, string Value) : OsmChangeAction;