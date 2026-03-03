namespace Osmalyzer;

public abstract class SuggestedAction
{
    public OsmElement.OsmElementType ElementType { get; }

    public long Id { get; }

    
    protected SuggestedAction(OsmElement.OsmElementType elementType, long id)
    {
        ElementType = elementType;
        Id = id;
    }
}

public class OsmSetValueSuggestedAction : SuggestedAction
{
    public string Key { get; }
    public string Value { get; }


    public OsmSetValueSuggestedAction(OsmElement.OsmElementType elementType, long id, string key, string value)
        : base(elementType, id)
    {
        Key = key;
        Value = value;
    }

    public OsmSetValueSuggestedAction(OsmElement element, string key, string value) 
        : this(element.ElementType, element.Id, key, value) { }
}

public class OsmRemoveKeySuggestedAction : SuggestedAction
{
    public string Key { get; }

    public OsmRemoveKeySuggestedAction(OsmElement.OsmElementType elementType, long id, string key)
        : base(elementType, id)
    {
        Key = key;
    }

    public OsmRemoveKeySuggestedAction(OsmElement element, string key) 
        : this(element.ElementType, element.Id, key) { }
}

public class OsmChangeKeySuggestedAction : SuggestedAction
{
    public string OldKey { get; }

    public string NewKey { get; }

    public string Value { get; }

    
    public OsmChangeKeySuggestedAction(OsmElement.OsmElementType elementType, long id, string oldKey, string newKey, string value)
        : base(elementType, id)
    {
        OldKey = oldKey;
        NewKey = newKey;
        Value = value;
    }

    public OsmChangeKeySuggestedAction(OsmElement element, string oldKey, string newKey, string value) 
        : this(element.ElementType, element.Id, oldKey, newKey, value) { }
}

public class OsmCreateNodeAction : SuggestedAction
{
    public OsmCoord Coord { get; }
    
    
    private static long _nextId = -1;
    

    public OsmCreateNodeAction(OsmCoord coord)
        : base(OsmElement.OsmElementType.Node, GetNextNewId())
    {
        Coord = coord;
    }

    
    [MustUseReturnValue]
    private static long GetNextNewId()
    {
        return _nextId--;
    }
}