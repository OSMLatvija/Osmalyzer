using System;

namespace Osmalyzer;

public abstract record OsmChangeAction(OsmElement Element);

public record OsmChangeCreateAction : OsmChangeAction
{
    public OsmChangeCreateAction(OsmElement Element)
        : base(Element)
    {
        if (Element.Id >= 0) throw new ArgumentException("Element to create must have a temporary (negative) ID.");
    }
}

public record OsmChangeModifyAction(OsmElement Element) : OsmChangeAction(Element);

public record OsmChangeDeleteAction(OsmElement Element, bool IfUnused = false) : OsmChangeAction(Element);
