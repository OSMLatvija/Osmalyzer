namespace Osmalyzer;

/// <summary>
/// Base class for user-facing osmChange actions
/// </summary>
public abstract record OsmChangeAction
{
    /// <summary>
    /// The element being affected by this action
    /// </summary>
    public OsmElement Element { get; }
    
    /// <summary>
    /// Changeset ID for this action
    /// </summary>
    public long Changeset { get; }
    
    protected OsmChangeAction(OsmElement element, long changeset)
    {
        Element = element;
        Changeset = changeset;
    }
}


/// <summary>
/// Set a single tag value on an element (will generate a modify action in XML)
/// </summary>
public record OsmSetValueAction(OsmElement Element, long Changeset, string Key, string Value) : OsmChangeAction(Element, Changeset);


// XML-specific action types - completely separate from user actions, used only internally by ToXml()

internal abstract record XmlAction(OsmElement Element, long Changeset);

internal record XmlCreateAction(OsmElement Element, long Changeset) : XmlAction(Element, Changeset);

internal record XmlModifyAction(OsmElement Element, long Changeset) : XmlAction(Element, Changeset);

internal record XmlDeleteAction(OsmElement Element, long Changeset, bool IfUnused = false) : XmlAction(Element, Changeset);
