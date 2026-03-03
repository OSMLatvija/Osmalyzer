namespace Osmalyzer;

public abstract record SuggestedAction(OsmElement.OsmElementType ElementType, long Id);

public record OsmSetValueSuggestedAction(OsmElement.OsmElementType ElementType, long Id, string Key, string Value) : SuggestedAction(ElementType, Id)
{
    public OsmSetValueSuggestedAction(OsmElement element, string key, string value) : this(element.ElementType, element.Id, key, value) { }
}

public record OsmRemoveKeySuggestedAction(OsmElement.OsmElementType ElementType, long Id, string Key) : SuggestedAction(ElementType, Id)
{
    public OsmRemoveKeySuggestedAction(OsmElement element, string key) : this(element.ElementType, element.Id, key) { }
}

public record OsmChangeKeySuggestedAction(OsmElement.OsmElementType ElementType, long Id, string OldKey, string NewKey, string Value) : SuggestedAction(ElementType, Id)
{
    public OsmChangeKeySuggestedAction(OsmElement element, string oldKey, string newKey, string value) : this(element.ElementType, element.Id, oldKey, newKey, value) { }
}

public record OsmCreateElementAction(OsmElement.OsmElementType ElementType, long Id) : SuggestedAction(ElementType, Id)
{
}