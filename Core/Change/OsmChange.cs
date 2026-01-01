using System;

namespace Osmalyzer;

public class OsmChange
{
    public IReadOnlyList<OsmChangeAction> Actions => _actions;

    
    private readonly List<OsmChangeAction> _actions;

    
    public OsmChange(List<OsmChangeAction> actions)
    {
        if (actions == null) throw new ArgumentNullException(nameof(actions));
        if (actions.Count == 0) throw new ArgumentException("Actions list cannot be empty.", nameof(actions));
        
        _actions = actions;
    }

    
    [Pure]
    public string ToXml()
    {
        // todo:
        return "";
    }
}