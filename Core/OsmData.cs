using System;
using System.Linq;

namespace Osmalyzer;

/// <summary>
/// 
/// </summary>
public abstract class OsmData
{
    [PublicAPI]
    public IReadOnlyList<OsmElement> Elements => _elements.AsReadOnly();

    [PublicAPI]
    public IReadOnlyList<OsmNode> Nodes => _nodes.AsReadOnly();

    [PublicAPI]
    public IReadOnlyList<OsmWay> Ways => _ways.AsReadOnly();

    [PublicAPI]
    public IReadOnlyList<OsmRelation> Relations => _relations.AsReadOnly();

    [PublicAPI]
    public int Count => _elements.Count;

        
    private List<OsmElement> _elements = null!; // will be set by whichever child constructor
        
    private List<OsmNode> _nodes = null!;
    private List<OsmNode> _nodesWithTags = null!;
    private List<OsmWay> _ways = null!;
    private List<OsmWay> _waysWithTags = null!;
    private List<OsmRelation> _relations = null!;
    private List<OsmRelation> _relationsWithTags = null!;
    private List<OsmElement> _elementsWithTags = null!;

    protected Dictionary<long, OsmNode> nodesById = null!;
    protected Dictionary<long, OsmWay> waysById = null!;
    protected Dictionary<long, OsmRelation> relationsById = null!;
    
    private Chunker<OsmElement>? _chunker;


    [Pure]
    public OsmDataExtract Filter(params OsmFilter[] filters)
    {
        List<OsmElement> filteredElements = [ ];

        IEnumerable<OsmElement> collection = ChooseCollectionForFiltering(filters);

        foreach (OsmElement element in collection)
            if (OsmElementMatchesFilters(element, filters))
                filteredElements.Add(element);

        return new OsmDataExtract(GetFullData(), filteredElements);
    }

    [Pure]
    public List<OsmDataExtract> Filter(List<OsmFilter[]> filters)
    {
        List<OsmDataExtract> extracts = new List<OsmDataExtract>(filters.Count);

        for (int i = 0; i < filters.Count; i++)
        {
            IEnumerable<OsmElement> collection = ChooseCollectionForFiltering(filters[i]);

            List<OsmElement> filteredElements = [ ];
                
            foreach (OsmElement element in collection)
                if (OsmElementMatchesFilters(element, filters[i]))
                    filteredElements.Add(element);
                
            extracts.Add(new OsmDataExtract(GetFullData(), filteredElements));
        }

        return extracts;
    }

    /// <summary>
    /// Remove duplicate elements based on the given similarity comparer.
    /// </summary>
    [Pure]
    public OsmDataExtract Deduplicate(Func<OsmElement, OsmElement, OsmElement?> similarityComparer)
    {
        List<OsmElement> elements = _elements.ToList();

        List<OsmElement> uniqueElements = [ ];

        for (int i = 0; i < elements.Count; i++)
        {
            bool isDuplicate = false;
            
            for (int k = i + 1; k < elements.Count; k++)
            {
                OsmElement? duplicate = similarityComparer(elements[i], elements[k]);
                
                if (duplicate == null)
                    continue;

                if (duplicate == elements[i])
                {
                    elements[k].AddDuplicates(elements[i]);
                    
                    isDuplicate = true;
                    break;
                }

                if (duplicate == elements[k])
                {
                    elements[i].AddDuplicates(elements[k]);

                    elements.RemoveAt(k);
                    k--;
                    // continue checking the rest
                }
                else
                {
                    throw new Exception("Similarity comparer returned an element that is neither of the two compared elements.");
                }
            }
            
            if (!isDuplicate)
                uniqueElements.Add(elements[i]);
        }

        return new OsmDataExtract(GetFullData(), uniqueElements);
    }

    public OsmElement? Find(params OsmFilter[] filters)
    {
        IEnumerable<OsmElement> collection = ChooseCollectionForFiltering(filters);
        
        foreach (OsmElement element in collection)
            if (OsmElementMatchesFilters(element, filters))
                return element;

        return null;
    }

    /// <summary>
    ///
    /// 1 2 3 4 - 2 3 5 = 1 4
    /// </summary>
    [Pure]
    public OsmDataExtract Subtract(OsmData other)
    {
        List<OsmElement> remainingElements = [ ];

        foreach (OsmElement element in _elements)
            if (!other._elements.Contains(element))
                remainingElements.Add(element);

        return new OsmDataExtract(GetFullData(), remainingElements);
    }


    /// <param name="split">Split semicolon-delimited OSM values, e.g. "gravel;asphalt". This is only useful for tags that are actually allowed top have multiple values.</param>
    [Pure]
    public OsmGroups GroupByValues(string key, bool split)
    {
        return GroupByValues(
            e => e.GetValue(key), 
            split
        );
    }

    /// <param name="split">Split semicolon-delimited OSM values, e.g. "gravel;asphalt". This is only useful for tags that are actually allowed top have multiple values.</param>
    [Pure]
    public OsmGroups GroupByValues(List<string> keys, bool split)
    {
        return GroupByValues(
            e =>
            {
                foreach (string tag in keys)
                    if (e.HasKey(tag))
                        return e.GetValue(tag);

                return null;
            }, 
            split
        );
    }

    [Pure]
    private OsmGroups GroupByValues(Func<OsmElement, string?> keySelector, bool split)
    {
        List<OsmGroup> groups = [ ];

        Dictionary<string, int> indices = new Dictionary<string, int>(); // for fast lookup

        List<string> values = [ ];

        foreach (OsmElement element in _elements)
        {
            if (element.HasAnyTags)
            {
                string? selectedValue = keySelector(element);

                if (selectedValue != null)
                {
                    values.Clear();

                    if (split)
                        values.AddRange(TagUtils.SplitValue(selectedValue));
                    else
                        values.Add(selectedValue);

                    // Make a new group for a new value or add the element to existing group
                        
                    foreach (string value in values)
                    {
                        if (indices.TryGetValue(value, out int index))
                        {
                            groups[index].Elements.Add(element);
                        }
                        else
                        {
                            OsmGroup newGroup = new OsmGroup(value);

                            newGroup.Elements.Add(element);

                            groups.Add(newGroup);

                            indices.Add(value, groups.Count - 1);
                        }
                    }
                }
            }
        }

        return new OsmGroups(groups);
    }

    [Pure]
    public List<string> GetUniqueValues(string tag)
    {
        List<string> values = [ ];

        foreach (OsmElement element in _elements)
        {
            if (element.HasKey(tag))
            {
                string value = element.GetValue(tag)!;

                if (!values.Contains(value))
                    values.Add(value);
            }
        }

        return values;
    }

    [Pure]
    [PublicAPI]
    public OsmElement? GetClosestElementTo(OsmCoord coord)
    {
        return GetClosestElementToRaw(coord, null, out _);
    }

    [Pure]
    [PublicAPI]
    public OsmElement? GetClosestElementTo(OsmCoord coord, double maxDistance)
    {
        _chunker ??= new Chunker<OsmElement>(_elements);

        return _chunker.GetClosest(coord.ToCartesian(), maxDistance);
    }

    [Pure]
    [PublicAPI]
    public OsmElement? GetClosestElementTo(OsmCoord coord, double maxDistance, out double? closestDistance)
    {
        return GetClosestElementToRaw(coord, maxDistance, out closestDistance);
    }

    [Pure]
    private OsmElement? GetClosestElementToRaw(OsmCoord coord, double? maxDistance, out double? closestDistance)
    {
        _chunker ??= new Chunker<OsmElement>(_elements);

        OsmElement? closest = _chunker.GetClosest(coord.ToCartesian(), maxDistance);
        
        // We also need to return the actual distance
        closestDistance = closest != null ? OsmGeoTools.DistanceBetween(coord, closest.AverageCoord) : null;
        
        return closest;
    }
        
        
    [Pure]
    [PublicAPI]
    public List<OsmNode> GetClosestNodesTo(OsmCoord coord, double maxDistance)
    {
        return GetClosestElementsTo(coord, maxDistance).OfType<OsmNode>().ToList();
    }
        
    [Pure]
    [PublicAPI]
    public List<OsmWay> GetClosestWaysTo(OsmCoord coord, double maxDistance)
    {
        return GetClosestElementsTo(coord, maxDistance).OfType<OsmWay>().ToList();
    }
        
    [Pure]
    [PublicAPI]
    public List<OsmRelation> GetClosestRelationsTo(OsmCoord coord, double maxDistance)
    {
        return GetClosestElementsTo(coord, maxDistance).OfType<OsmRelation>().ToList();
    }
        
    [Pure]
    [PublicAPI]
    public List<OsmElement> GetClosestElementsTo(OsmCoord coord, double maxDistance)
    {
        return GetClosestElementsToRaw(coord, maxDistance);
    }

    [Pure]
    [PublicAPI]
    private List<OsmElement> GetClosestElementsToRaw(OsmCoord coord, double? maxDistance)
    {
        _chunker ??= new Chunker<OsmElement>(_elements);
        
        return _chunker.GetAllClosest(coord.ToCartesian(), maxDistance);
    }

    [Pure]
    public OsmChange GetChanges()
    {
        return new OsmChange(this);
    }

    public OsmNode CreateNewNode(OsmCoord coord)
    {
        OsmNode newNode = new OsmNode(coord);
        AddElement(newNode);
        return newNode;
    }

    /// <summary>
    /// Makes a deep copy of all the data.
    /// Any changes to the elements of the copied data will not affect the original data.
    /// </summary>
    [Pure]
    public OsmData Copy()
    {
        // Create dictionaries to map old elements to new elements
        Dictionary<long, OsmNode> newNodesById = new Dictionary<long, OsmNode>(_nodes.Count);
        Dictionary<long, OsmWay> newWaysById = new Dictionary<long, OsmWay>(_ways.Count);
        Dictionary<long, OsmRelation> newRelationsById = new Dictionary<long, OsmRelation>(_relations.Count);

        // Create copies of all nodes (without backlinks yet)
        foreach (OsmNode node in _nodes)
        {
            OsmNode newNode = CopyNode(node);
            newNodesById.Add(node.Id, newNode);
        }

        // Create copies of all ways (with links to new nodes, without backlinks yet)
        foreach (OsmWay way in _ways)
        {
            OsmWay newWay = CopyWay(way, newNodesById);
            newWaysById.Add(way.Id, newWay);
        }

        // Create copies of all relations (with links to new elements, without backlinks yet)
        foreach (OsmRelation relation in _relations)
        {
            OsmRelation newRelation = CopyRelation(relation, newNodesById, newWaysById, newRelationsById);
            newRelationsById.Add(relation.Id, newRelation);
        }

        // Establish backlinks for ways in nodes
        foreach (OsmWay way in _ways)
        {
            OsmWay newWay = newWaysById[way.Id];

            foreach (OsmNode node in way.nodes)
            {
                OsmNode newNode = newNodesById[node.Id];

                if (newNode.ways == null)
                    newNode.ways = [ ];

                newNode.ways.Add(newWay);
            }
        }

        // Establish backlinks for relations
        foreach (OsmRelation relation in _relations)
        {
            OsmRelation newRelation = newRelationsById[relation.Id];

            foreach (OsmRelationMember member in relation.members)
            {
                if (member.Element == null)
                    continue;

                OsmRelationMember newMember = newRelation.members.First(m => m.Id == member.Id && m.ElementType == member.ElementType);

                switch (member.Element)
                {
                    case OsmNode node:
                        if (newNodesById.TryGetValue(node.Id, out OsmNode? newNode))
                        {
                            if (newNode.relations == null)
                                newNode.relations = [ ];
                            newNode.relations.Add(newMember);
                        }

                        break;

                    case OsmWay way:
                        if (newWaysById.TryGetValue(way.Id, out OsmWay? newWay))
                        {
                            if (newWay.relations == null)
                                newWay.relations = [ ];
                            newWay.relations.Add(newMember);
                        }

                        break;

                    case OsmRelation rel:
                        if (newRelationsById.TryGetValue(rel.Id, out OsmRelation? newRel))
                        {
                            if (newRel.relations == null)
                                newRel.relations = [ ];
                            newRel.relations.Add(newMember);
                        }

                        break;
                }
            }
        }

        // Step 6: Create the result data structure
        List<OsmElement> newElements = [ ];
        newElements.AddRange(newNodesById.Values);
        newElements.AddRange(newWaysById.Values);
        newElements.AddRange(newRelationsById.Values);

        if (this is OsmDataExtract extract)
            return new OsmDataExtract(extract.FullData, newElements);
        else
            return new OsmDataExtract((OsmMasterData)this, newElements);
        

        [Pure]
        static OsmNode CopyNode(OsmNode original)
        {
            OsmNode copy = new OsmNode(original);
            return copy;
        }

        [Pure]
        static OsmWay CopyWay(OsmWay original, Dictionary<long, OsmNode> newNodesById)
        {
            OsmWay copy = new OsmWay(original);

            // Link to new nodes
            foreach (OsmNode node in original.nodes)
                copy.nodes.Add(newNodesById[node.Id]);

            return copy;
        }

        [Pure]
        static OsmRelation CopyRelation(OsmRelation original, Dictionary<long, OsmNode> newNodesById, Dictionary<long, OsmWay> newWaysById, Dictionary<long, OsmRelation> newRelationsById)
        {
            OsmRelation copy = new OsmRelation(original);

            // Link members to new elements
            foreach (OsmRelationMember member in copy.members)
            {
                switch (member.ElementType)
                {
                    case OsmRelationMember.MemberElementType.Node:
                        if (newNodesById.TryGetValue(member.Id, out OsmNode? newNode))
                            member.Element = newNode;
                        break;

                    case OsmRelationMember.MemberElementType.Way:
                        if (newWaysById.TryGetValue(member.Id, out OsmWay? newWay))
                            member.Element = newWay;
                        break;

                    case OsmRelationMember.MemberElementType.Relation:
                        if (newRelationsById.TryGetValue(member.Id, out OsmRelation? newRelation))
                            member.Element = newRelation;
                        break;
                }
            }

            return copy;
        }
    }

    [Pure]
    public OsmNode GetNodeById(long id) => nodesById[id];
    
    [Pure]
    public OsmWay GetWayById(long id) => waysById[id];
   
    [Pure]
    public OsmRelation GetRelationById(long id) => relationsById[id];

    [Pure]
    public OsmElement GetElementById(OsmElement.OsmElementType type, long id)
    {
        return type switch
        {
            OsmElement.OsmElementType.Node     => GetNodeById(id),
            OsmElement.OsmElementType.Way      => GetWayById(id),
            OsmElement.OsmElementType.Relation => GetRelationById(id),
            _                                  => throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported OSM element type: {type}"),
        };
    }


    [Pure]
    protected static bool OsmElementMatchesFilters(OsmElement element, params OsmFilter[] filters)
    {
        bool matched = true;

        foreach (OsmFilter filter in filters)
        {
            if (!filter.Matches(element))
            {
                matched = false;
                break;
            }
        }

        return matched;
    }
        

    [Pure]
    private OsmMasterData GetFullData()
    {
        return this is OsmDataExtract ed ? ed.FullData : (OsmMasterData)this;
    }

    protected void CreateElements(int? capacity, int? nodeCapacity, int? wayCapacity, int? relationCapacity)
    {
        _elements =
            capacity != null ?
                new List<OsmElement>(capacity.Value) :
                [ ];

        _nodes =
            nodeCapacity != null ?
                new List<OsmNode>(nodeCapacity.Value) :
                [ ];

        _ways =
            wayCapacity != null ?
                new List<OsmWay>(wayCapacity.Value) :
                [ ];

        _relations =
            relationCapacity != null ?
                new List<OsmRelation>(relationCapacity.Value) :
                [ ];
        
        nodesById = 
            nodeCapacity != null ?
                new Dictionary<long, OsmNode>(nodeCapacity.Value) :
                new Dictionary<long, OsmNode>();
        
        waysById =
            wayCapacity != null ?
                new Dictionary<long, OsmWay>(wayCapacity.Value) :
                new Dictionary<long, OsmWay>();
        
        relationsById =
            relationCapacity != null ?
                new Dictionary<long, OsmRelation>(relationCapacity.Value) :
                new Dictionary<long, OsmRelation>();

        _nodesWithTags = [ ];
        _waysWithTags = [ ];
        _relationsWithTags = [ ];
        _elementsWithTags = [ ];
    }

    protected void AddElement(OsmElement newElement)
    {
        _elements.Add(newElement);

        bool hasAnyTags = newElement.HasAnyTags;
        
        if (hasAnyTags)
            _elementsWithTags.Add(newElement);
        
        switch (newElement)
        {
            case OsmNode node:
                _nodes.Add(node);
                nodesById.Add(node.Id, node);
                    
                if (hasAnyTags)
                    _nodesWithTags.Add(node);
                break;

            case OsmWay way:
                _ways.Add(way);
                waysById.Add(way.Id, way);
                    
                if (hasAnyTags)
                    _waysWithTags.Add(way);
                break;
                
            case OsmRelation relation:
                _relations.Add(relation);
                relationsById.Add(relation.Id, relation);
                    
                if (hasAnyTags)
                    _relationsWithTags.Add(relation);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(newElement));
        }
    }

    [Pure]
    private IEnumerable<OsmElement> ChooseCollectionForFiltering(OsmFilter[] filters)
    {
        bool nodesOnly = filters.Any(f => f.ForNodesOnly);
        bool waysOnly = filters.Any(f => f.ForWaysOnly);
        bool relationsOnly = filters.Any(f => f.ForRelationsOnly);
        bool taggedOnly = filters.Any(f => f.TaggedOnly);
        // todo: go through filters one at a time and keep flags instead of multiple enumerations

        // todo: all the combos, like nodes + ways
        
        if (nodesOnly)
        {
            if (taggedOnly)
                return _nodesWithTags;

            return _nodes;
        }

        if (waysOnly)
        {
            if (taggedOnly)
                return _waysWithTags;

            return _ways;
        }

        if (relationsOnly)
        {
            if (taggedOnly)
                return _relationsWithTags;

            return _relations;
        }
        
        if (taggedOnly)
            return _elementsWithTags;

        // todo: return back a new filter list without the redundant filter we dismissed
        
        return _elements;
    }
}