using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace Osmalyzer
{
    public class OsmBlob
    {
        public IReadOnlyList<OsmElement> Elements => _elements.AsReadOnly();


        private readonly List<OsmElement> _elements;

        public readonly Dictionary<long, OsmElement> _elementsById = new Dictionary<long, OsmElement>();
        

        public OsmBlob(string dataFileName, params OsmFilter[] filters)
        {
            _elements = new List<OsmElement>();

            using FileStream fileStream = new FileInfo(dataFileName).OpenRead();

            using PBFOsmStreamSource source = new PBFOsmStreamSource(fileStream);

            foreach (OsmGeo element in source)
            {
                if (OsmElementMatchesFilters(element, filters))
                {
                    OsmElement osmElement = OsmElement.Create(element);
                    _elements.Add(osmElement);
                    _elementsById.Add(osmElement.Id, osmElement);
                }
            }
        }


        private OsmBlob(List<OsmElement> elements)
        {
            _elements = elements;

            foreach (OsmElement element in _elements)
                _elementsById.Add(element.Id, element);
        }

        
        public static List<OsmBlob> CreateMultiple(string dataFileName, List<OsmFilter[]> filters)
        {
            List<List<OsmElement>> elements = new List<List<OsmElement>>();
            for (int i = 0; i < filters.Count; i++)
                elements.Add(new List<OsmElement>());

            using FileStream fileStream = new FileInfo(dataFileName).OpenRead();

            using PBFOsmStreamSource source = new PBFOsmStreamSource(fileStream);

            foreach (OsmGeo element in source)
                for (int i = 0; i < filters.Count; i++)
                    if (OsmElementMatchesFilters(element, filters[i]))
                    {
                        OsmElement osmElement = OsmElement.Create(element);
                        elements[i].Add(osmElement);
                    }

            List<OsmBlob> blobs = new List<OsmBlob>();

            for (int i = 0; i < filters.Count; i++)
                blobs.Add(new OsmBlob(elements[i]));

            return blobs;
        }


        [Pure]
        public OsmBlob Filter(params OsmFilter[] filters)
        {
            List<OsmElement> filteredElements = new List<OsmElement>();

            foreach (OsmElement element in _elements)
                if (OsmElementMatchesFilters(element.Element, filters))
                    filteredElements.Add(element);

            return new OsmBlob(filteredElements);
        }

        /// <param name="split">Split semicolon-delimited OSM values, e.g. "gravel;asphalt". This is only useful for tags that are actually allowed top have multiple values.</param>
        public OsmGroups GroupByValues(string tag, bool split)
        {
            return GroupByValues(
                e => e.Element.Tags.ContainsKey(tag) ? e.Element.Tags.GetValue(tag) : null, 
                split
            );
        }

        /// <param name="split">Split semicolon-delimited OSM values, e.g. "gravel;asphalt". This is only useful for tags that are actually allowed top have multiple values.</param>
        public OsmGroups GroupByValues(List<string> tags, bool split)
        {
            return GroupByValues(
                e =>
                {
                    foreach (string tag in tags)
                        if (e.Element.Tags.ContainsKey(tag))
                            return e.Element.Tags.GetValue(tag);

                    return null;
                }, 
                split
            );
        }

        private OsmGroups GroupByValues(Func<OsmElement, string?> keySelector, bool split)
        {
            List<OsmGroup> groups = new List<OsmGroup>();

            Dictionary<string, int> indices = new Dictionary<string, int>(); // for fast lookup

            List<string> values = new List<string>();

            foreach (OsmElement element in _elements)
            {
                if (element.Element.Tags != null)
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

        public List<string> GetUniqueValues(string tag)
        {
            List<string> values = new List<string>();

            foreach (OsmElement element in _elements)
            {
                if (element.Element.Tags != null &&
                    element.Element.Tags.ContainsKey(tag))
                {
                    string value = element.Element.Tags.GetValue(tag);

                    if (!values.Contains(value))
                        values.Add(value);
                }
            }

            return values;
        }

        /// <summary>
        ///
        /// 1 2 3 4 - 2 3 5 = 1 4
        /// </summary>
        public OsmBlob Subtract(OsmBlob other)
        {
            List<OsmElement> elements = new List<OsmElement>();

            foreach (OsmElement element in Elements)
                if (!other.Elements.Contains(element))
                    elements.Add(element);

            return new OsmBlob(elements);
        }

        [Pure]
        public OsmNode? GetClosestNodeTo(double lat, double lon)
        {
            return GetClosestNodeToRaw(lat, lon, null, out _);
        }

        [Pure]
        public OsmNode? GetClosestNodeTo(double lat, double lon, double maxDistance)
        {
            return GetClosestNodeToRaw(lat, lon, maxDistance, out _);
        }

        [Pure]
        public OsmNode? GetClosestNodeTo(double lat, double lon, double maxDistance, out double? closestDistance)
        {
            return GetClosestNodeToRaw(lat, lon, maxDistance, out closestDistance);
        }

        [Pure]
        private OsmNode? GetClosestNodeToRaw(double lat, double lon, double? maxDistance, out double? closestDistance)
        {
            OsmNode? bestNode = null;
            double bestDistance = 0.0;
            closestDistance = null;

            foreach (OsmElement element in _elements)
            {
                if (element is not OsmNode node)
                    continue; // only care about nodes
                
                double distance = OsmGeoTools.DistanceBetween(
                    lat, lon,
                    node.Lat, node.Lon 
                );

                if (maxDistance == null || distance <= maxDistance) // within max distance
                {
                    if (bestNode == null || bestDistance > distance)
                    {
                        bestNode = node;
                        bestDistance = distance;
                        closestDistance = distance;
                    }
                }
                else if (closestDistance == null || distance <= closestDistance)
                {
                    closestDistance = distance;
                }
            }

            return bestNode;
        }
        
        
        [Pure]
        public List<OsmNode> GetClosestNodesTo(double lat, double lon, double maxDistance)
        {
            return GetClosestNodesToRaw(lat, lon, maxDistance);
        }

        [Pure]
        private List<OsmNode> GetClosestNodesToRaw(double lat, double lon, double? maxDistance)
        {
            List<(double, OsmNode)> nodes = new List<(double, OsmNode)>(); // todo: presorted collection

            foreach (OsmElement element in _elements)
            {
                if (element is not OsmNode node)
                    continue; // only care about nodes
                
                double distance = OsmGeoTools.DistanceBetween(
                    lat, lon,
                    node.Lat, node.Lon 
                );

                if (maxDistance == null || distance <= maxDistance) // within max distance
                {
                    nodes.Add((distance, node));
                }
            }

            return nodes.OrderBy(n => n.Item1).Select(n => n.Item2).ToList();
        }

        
        [Pure]
        private static bool OsmElementMatchesFilters(OsmGeo element, params OsmFilter[] filters)
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
    }

    public class OsmGroups
    {
        public readonly List<OsmGroup> groups;


        public OsmGroups(List<OsmGroup> groups)
        {
            this.groups = groups;
        }


        public void SortGroupsByElementCountAsc()
        {
            groups.Sort((g1, g2) => g1.Elements.Count.CompareTo(g2.Elements.Count));
        }

        public void SortGroupsByElementCountDesc()
        {
            groups.Sort((g1, g2) => g2.Elements.Count.CompareTo(g1.Elements.Count));
        }

        public OsmMultiValueGroups CombineBySimilarValues(Func<string, string, bool> valueMatcher)
        {
            List<OsmMultiValueGroup> multiGroups = new List<OsmMultiValueGroup>();

            bool[] processed = new bool[groups.Count];

            for (int g1 = 0; g1 < groups.Count; g1++)
            {
                if (!processed[g1]) // otherwise, already added to another earlier group
                {
                    OsmGroup group1 = groups[g1];

                    OsmMultiValueGroup multiGroup = new OsmMultiValueGroup();
                    
                    // Add elements from group 1 - these always go in 
                    
                    multiGroup.Values.Add(group1.Value);
                    multiGroup.ElementCounts.Add(group1.Elements.Count);
                    foreach (OsmElement element in group1.Elements)
                        multiGroup.Elements.Add(new OsmMultiValueElement(group1.Value, element));

                    // Find any similar groups
                    
                    for (int g2 = g1 + 1; g2 < groups.Count; g2++)
                    {
                        if (!processed[g2]) // otherwise, already added to another earlier group
                        {
                            OsmGroup group2 = groups[g2];

                            if (valueMatcher(group1.Value, group2.Value))
                            {
                                // Add elements from group 2 since they matched
                    
                                multiGroup.Values.Add(group2.Value);
                                multiGroup.ElementCounts.Add(group2.Elements.Count);
                                foreach (OsmElement element in group2.Elements)
                                    multiGroup.Elements.Add(new OsmMultiValueElement(group2.Value, element));

                                processed[g2] = true;
                            }
                        }
                    }

                    processed[g1] = true; // although this is pointless since we never "go back"

                    multiGroups.Add(multiGroup);
                }
            }

            return new OsmMultiValueGroups(multiGroups);
        }
    }

    public class OsmGroup
    {
        public string Value { get; }

        public List<OsmElement> Elements { get; } = new List<OsmElement>();


        public OsmGroup(string value)
        {
            Value = value;
        }
    }

    public class OsmMultiValueGroups
    {
        public readonly List<OsmMultiValueGroup> groups;


        public OsmMultiValueGroups(List<OsmMultiValueGroup> groups)
        {
            this.groups = groups;
        }

        
        public void SortGroupsByElementCountAsc()
        {
            groups.Sort((g1, g2) => g1.Elements.Count.CompareTo(g2.Elements.Count));
        }

        public void SortGroupsByElementCountDesc()
        {
            groups.Sort((g1, g2) => g2.Elements.Count.CompareTo(g1.Elements.Count));
        }
    }

    public class OsmMultiValueGroup
    {
        /// <summary> All the unique values </summary>
        public List<string> Values { get; } = new List<string>();

        public List<OsmMultiValueElement> Elements { get; } = new List<OsmMultiValueElement>();

        /// <summary> Element counts for each unique value </summary>
        public List<int> ElementCounts { get; } = new List<int>();

        
        public List<string> GetUniqueKeyValues(string key, bool sort = false)
        {
            List<string> used = new List<string>();

            foreach (OsmMultiValueElement element in Elements)
                foreach (Tag tag in element.Element.Element.Tags)
                    if (tag.Key == key)
                        if (!used.Contains(tag.Value))
                            used.Add(tag.Value);

            if (sort)
                used.Sort();
            
            return used;
        }
    }

    public class OsmMultiValueElement
    {
        /// <summary> The group's value that had this element </summary>
        public string Value { get; }

        public OsmElement Element { get; }


        public OsmMultiValueElement(string value, OsmElement element)
        {
            Value = value;
            Element = element;
        }
    }

    public abstract class OsmElement
    {
        public long Id => Element.Id!.Value; 
        
        
        internal OsmGeo Element { get; }


        protected OsmElement(OsmGeo element)
        {
            Element = element;
        }


        internal static OsmElement Create(OsmGeo element)
        {
            switch (element.Type)
            {
                case OsmGeoType.Node:     return new OsmNode(element);
                case OsmGeoType.Way:      return new OsmWay(element);
                case OsmGeoType.Relation: return new OsmRelation(element);
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        
        [Pure]
        public string? GetValue(string key)
        {
            return Element.Tags.ContainsKey(key) ? Element.Tags.GetValue(key) : null;
        }
        
        [Pure]
        public bool HasValue(string key)
        {
            return Element.Tags.ContainsKey(key);
        }
    }

    public class OsmWay : OsmElement
    {
        internal OsmWay(OsmGeo element)
            : base(element)
        {
        }
    }

    public class OsmNode : OsmElement
    {
        public double Lat => ((Node)Element).Latitude!.Value;
        
        public double Lon => ((Node)Element).Longitude!.Value;
        
        
        internal OsmNode(OsmGeo element)
            : base(element)
        {
        }
    }

    public class OsmRelation : OsmElement
    {
        public IEnumerable<OsmRelationMember> Members => ((Relation)Element).Members.Select(m => new OsmRelationMember(m.Id, m.Role));


        internal OsmRelation(OsmGeo element)
            : base(element)
        {
        }
    }

    public class OsmRelationMember
    {
        public long Id { get; }
        
        public string Role { get; }
        

        internal OsmRelationMember(long id, string role)
        {
            Id = id;
            Role = role;
        }
    }

    public abstract class OsmFilter
    {
        internal abstract bool Matches(OsmGeo element);
    }

    public class OrMatch : OsmFilter
    {
        private readonly OsmFilter[] _filters;

        
        public OrMatch(params OsmFilter[] filters)
        {
            _filters = filters;
        }
        
        
        internal override bool Matches(OsmGeo element)
        {
            return _filters.Any(f => f.Matches(element));
        }
    }

    public class AndMatch : OsmFilter
    {
        private readonly OsmFilter[] _filters;

        
        public AndMatch(params OsmFilter[] filters)
        {
            _filters = filters;
        }
        
        
        internal override bool Matches(OsmGeo element)
        {
            return _filters.All(f => f.Matches(element));
        }
    }

    public class IsNode : OsmFilter
    {
        internal override bool Matches(OsmGeo element)
        {
            return element.Type == OsmGeoType.Node;
        }
    }

    public class IsWay : OsmFilter
    {
        internal override bool Matches(OsmGeo element)
        {
            return element.Type == OsmGeoType.Way;
        }
    }

    public class IsRelation : OsmFilter
    {
        internal override bool Matches(OsmGeo element)
        {
            return element.Type == OsmGeoType.Relation;
        }
    }

    public class IsNodeOrWay : OsmFilter
    {
        internal override bool Matches(OsmGeo element)
        {
            return 
                element.Type == OsmGeoType.Node ||
                element.Type == OsmGeoType.Way;
        }
    }

    public class HasTag : OsmFilter
    {
        private readonly string _tag;


        public HasTag(string tag)
        {
            _tag = tag;
        }


        internal override bool Matches(OsmGeo element)
        {
            return
                element.Tags != null &&
                element.Tags.ContainsKey(_tag);
        }
    }

    public class HasAnyTag : OsmFilter
    {
        private readonly List<string> _tags;


        public HasAnyTag(List<string> tags)
        {
            _tags = tags;
        }


        internal override bool Matches(OsmGeo element)
        {
            return
                element.Tags != null &&
                element.Tags.Any(t => _tags.Contains(t.Key));
        }
    }

    public class DoesntHaveTag : OsmFilter
    {
        private readonly string _tag;


        public DoesntHaveTag(string tag)
        {
            _tag = tag;
        }


        internal override bool Matches(OsmGeo element)
        {
            return
                element.Tags == null ||
                !element.Tags.ContainsKey(_tag);
        }
    }

    public class HasValue : OsmFilter
    {
        private readonly string _tag;
        private readonly string _value;


        public HasValue(string tag, string value)
        {
            _tag = tag;
            _value = value;
        }


        internal override bool Matches(OsmGeo element)
        {
            return
                element.Tags != null &&
                element.Tags.Contains(_tag, _value);
        }
    }

    public class HasAnyValue : OsmFilter
    {
        private readonly string _tag;
        private readonly List<string> _values;


        public HasAnyValue(string tag, List<string> values)
        {
            _tag = tag;
            _values = values;
        }


        internal override bool Matches(OsmGeo element)
        {
            return
                element.Tags != null &&
                element.Tags.Any(t => t.Key == _tag && _values.Contains(t.Value));
        }
    }

    public class SplitValuesMatchRegex : OsmFilter
    {
        private readonly string _tag;
        private readonly string _pattern;


        public SplitValuesMatchRegex(string tag, string pattern)
        {
            _tag = tag;
            _pattern = pattern;
        }


        internal override bool Matches(OsmGeo element)
        {
            if (element.Tags == null)
                return false;

            string rawValue = element.Tags.GetValue(_tag);

            List<string> splitValues = TagUtils.SplitValue(rawValue);

            if (splitValues.Count == 0)
                return false;

            foreach (string splitValue in splitValues)
                if (!Regex.IsMatch(splitValue, _pattern))
                    return false;

            return true;
        }
    }

    public class SplitValuesCheck : OsmFilter
    {
        private readonly string _tag;
        private readonly Func<string, bool> _check;


        public SplitValuesCheck(string tag, Func<string, bool> check)
        {
            _tag = tag;
            _check = check;
        }


        internal override bool Matches(OsmGeo element)
        {
            if (element.Tags == null)
                return false;

            string rawValue = element.Tags.GetValue(_tag);

            List<string> splitValues = TagUtils.SplitValue(rawValue);

            if (splitValues.Count == 0)
                return false;

            foreach (string splitValue in splitValues)
                if (!_check(splitValue))
                    return false;

            return true;
        }
    }
}