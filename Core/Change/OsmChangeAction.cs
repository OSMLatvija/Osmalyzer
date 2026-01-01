namespace Osmalyzer;

public abstract record OsmChangeAction(OsmElement Element);

public record OsmChangeCreateAction(OsmElement Element) : OsmChangeAction(Element);

public record OsmChangeModifyAction(OsmElement Element) : OsmChangeAction(Element);

public record OsmChangeDeleteAction(OsmElement Element, bool IfUnused = false) : OsmChangeAction(Element);
