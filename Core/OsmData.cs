using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer
{
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

        
        private List<OsmElement> _elements = null!; // will be set by whichever child constructor
        
        private List<OsmNode> _nodes = null!;
        private List<OsmWay> _ways = null!;
        private List<OsmRelation> _relations = null!;


        [Pure]
        public OsmDataExtract Filter(params OsmFilter[] filters)
        {
            List<OsmElement> filteredElements = new List<OsmElement>();

            foreach (OsmElement element in _elements)
                if (OsmElementMatchesFilters(element, filters))
                    filteredElements.Add(element);

            return new OsmDataExtract(GetFullData(), filteredElements);
        }

        [Pure]
        public List<OsmDataExtract> Filter(List<OsmFilter[]> filters)
        {
            List<List<OsmElement>> filteredElements = new List<List<OsmElement>>(filters.Count);
            for (int i = 0; i < filters.Count; i++)
                filteredElements.Add(new List<OsmElement>());

            foreach (OsmElement element in _elements)
                for (int i = 0; i < filters.Count; i++)
                    if (OsmElementMatchesFilters(element, filters[i]))
                        filteredElements[i].Add(element);

            List<OsmDataExtract> extracts = new List<OsmDataExtract>(filters.Count);

            for (int i = 0; i < filters.Count; i++)
                extracts.Add(new OsmDataExtract(GetFullData(), filteredElements[i]));

            return extracts;
        }
        
        public OsmElement? Find(params OsmFilter[] filters)
        {
            foreach (OsmElement element in _elements)
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
        public OsmGroups GroupByValues(string tag, bool split)
        {
            return GroupByValues(
                e => e.RawElement.Tags.ContainsKey(tag) ? e.RawElement.Tags.GetValue(tag) : null, 
                split
            );
        }

        /// <param name="split">Split semicolon-delimited OSM values, e.g. "gravel;asphalt". This is only useful for tags that are actually allowed top have multiple values.</param>
        [Pure]
        public OsmGroups GroupByValues(List<string> tags, bool split)
        {
            return GroupByValues(
                e =>
                {
                    foreach (string tag in tags)
                        if (e.RawElement.Tags.ContainsKey(tag))
                            return e.RawElement.Tags.GetValue(tag);

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
                if (element.RawElement.Tags != null)
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
                if (element.RawElement.Tags != null &&
                    element.RawElement.Tags.ContainsKey(tag))
                {
                    string value = element.RawElement.Tags.GetValue(tag);

                    if (!values.Contains(value))
                        values.Add(value);
                }
            }

            return values;
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
        }

        protected void AddElement(OsmElement newElement)
        {
            _elements.Add(newElement);
        }
    }
}