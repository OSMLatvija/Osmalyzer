namespace Osmalyzer;

internal abstract record OsmChangeAction(OsmElement Element);

internal record OsmChangeCreateAction(OsmElement Element) : OsmChangeAction(Element);

internal record OsmChangeModifyAction(OsmElement Element) : OsmChangeAction(Element);

internal record OsmChangeDeleteAction(OsmElement Element, bool IfUnused = false) : OsmChangeAction(Element);
