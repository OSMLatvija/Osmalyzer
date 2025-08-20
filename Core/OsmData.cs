using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

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

    private Chunker<OsmElement>? _chunker;


    [Pure]
    public OsmDataExtract Filter(params OsmFilter[] filters)
    {
        List<OsmElement> filteredElements = new List<OsmElement>();

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

            List<OsmElement> filteredElements = new List<OsmElement>();
                
            foreach (OsmElement element in collection)
                if (OsmElementMatchesFilters(element, filters[i]))
                    filteredElements.Add(element);
                
            extracts.Add(new OsmDataExtract(GetFullData(), filteredElements));
        }

        return extracts;
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
        List<OsmElement> remainingElements = new List<OsmElement>();

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
        List<OsmGroup> groups = new List<OsmGroup>();

        Dictionary<string, int> indices = new Dictionary<string, int>(); // for fast lookup

        List<string> values = new List<string>();

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
        List<string> values = new List<string>();

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
                new List<OsmElement>();

        _nodes =
            nodeCapacity != null ?
                new List<OsmNode>(nodeCapacity.Value) :
                new List<OsmNode>();

        _ways =
            wayCapacity != null ?
                new List<OsmWay>(wayCapacity.Value) :
                new List<OsmWay>();

        _relations =
            relationCapacity != null ?
                new List<OsmRelation>(relationCapacity.Value) :
                new List<OsmRelation>();

        _nodesWithTags = new List<OsmNode>();
        _waysWithTags = new List<OsmWay>();
        _relationsWithTags = new List<OsmRelation>();
        _elementsWithTags = new List<OsmElement>();
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
                    
                if (hasAnyTags)
                    _nodesWithTags.Add(node);
                break;

            case OsmWay way:
                _ways.Add(way);
                    
                if (hasAnyTags)
                    _waysWithTags.Add(way);
                break;
                
            case OsmRelation relation:
                _relations.Add(relation);
                    
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
        // todo: return back a new filter list without the redundant filter we dismissed
            
        // todo: go through filters one at a time and keep flags insetad of multiple enumerations
            
        if (filters.Any(f => f.ForNodesOnly))
        {
            if (filters.Any(f => f.TaggedOnly))
                return _nodesWithTags;

            return _nodes;
        }

        if (filters.Any(f => f.ForWaysOnly))
        {
            if (filters.Any(f => f.TaggedOnly))
                return _waysWithTags;

            return _ways;
        }

        if (filters.Any(f => f.ForRelationsOnly))
        {
            if (filters.Any(f => f.TaggedOnly))
                return _relationsWithTags;

            return _relations;
        }
        
        if (filters.Any(f => f.TaggedOnly))
            return _elementsWithTags;

        return _elements;
    }
}