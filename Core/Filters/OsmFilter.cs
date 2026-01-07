namespace Osmalyzer;

public abstract class OsmFilter
{
    /// <summary>
    /// Does this filter only apply to nodes?
    /// That is, only nodes will ever match this filter, so anything that isn't a node (way, relation) can be skipped/optimized out when matching.
    /// </summary>
    public abstract bool ForNodesOnly { get; }
    
    /// <summary>
    /// Does this filter only apply to ways?
    /// That is, only ways will ever match this filter, so anything that isn't a way (node, relation) can be skipped/optimized out when matching.
    /// </summary>
    public abstract bool ForWaysOnly { get; }
    
    /// <summary>
    /// Does this filter only apply to relations?
    /// That is, only relations will ever match this filter, so anything that isn't a relation (node, way) can be skipped/optimized out when matching.
    /// </summary>
    public abstract bool ForRelationsOnly { get; }
 
    /// <summary>
    /// Does this filter only apply to tagged elements?
    /// That is, only elements with at least one tag will ever match this filter, so anything that is untagged can be skipped/optimized out when matching.
    /// </summary>
    public abstract bool TaggedOnly { get; }


    internal abstract bool Matches(OsmElement element);
}