//#if !REMOTE_EXECUTION
#define BENCHMARK
using System.Diagnostics;
//#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using OsmSharp;
using OsmSharp.Streams;

namespace Osmalyzer
{
    /// <summary>
    /// Full list of OSM data, as loaded.
    /// </summary>
    public class OsmMasterData : OsmData
    {
        [PublicAPI]
        public override IReadOnlyList<OsmElement> Elements => _elements.AsReadOnly();


        private readonly List<OsmElement> _elements;

        
        internal ReadOnlyDictionary<long, OsmNode> NodesById => new ReadOnlyDictionary<long, OsmNode>(_nodesById);
        internal ReadOnlyDictionary<long, OsmWay> WaysById => new ReadOnlyDictionary<long, OsmWay>(_waysById);
        internal ReadOnlyDictionary<long, OsmRelation> RelationsById => new ReadOnlyDictionary<long, OsmRelation>(_relationsById);

        
        private readonly Dictionary<long, OsmNode> _nodesById = new Dictionary<long, OsmNode>();
        private readonly Dictionary<long, OsmWay> _waysById = new Dictionary<long, OsmWay>();
        private readonly Dictionary<long, OsmRelation> _relationsById = new Dictionary<long, OsmRelation>();
        

        public OsmMasterData(string dataFileName)
        {
#if BENCHMARK
            // As of last benchmark:
            // OSMSharp data loading took 15336 ms
            // OSM data conversion took 3090 ms
            // OSM data linking took 3574 ms
            
            // At this point, I cannot think of any (non micro-) optimization to do here.
            // The bulk of the work is 15 sec for the PBF file reading and processing,
            // so the remaining 6.5 sec are largely irrelevant then.

            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            
            // Read the "raw" elements from the file

            using FileStream fileStream = new FileInfo(dataFileName).OpenRead();

            using PBFOsmStreamSource source = new PBFOsmStreamSource(fileStream);

            List<OsmGeo> rawElements = source.ToList();

#if BENCHMARK
            stopwatch.Stop();
            Console.WriteLine("OSMSharp data loading took " + stopwatch.ElapsedMilliseconds + " ms");
            stopwatch.Restart();
#endif
            
            // Convert the "raw" elements to our own structure

            _elements = new List<OsmElement>();

            foreach (OsmGeo element in rawElements)
            {
                OsmElement osmElement = OsmElement.Create(element);

                _elements.Add(osmElement);

                switch (osmElement)
                {
                    case OsmNode osmNode:
                        _nodesById.Add(osmElement.Id, osmNode);
                        break;

                    case OsmWay osmWay:
                        _waysById.Add(osmElement.Id, osmWay);
                        break;
                    
                    case OsmRelation osmRelation:
                        _relationsById.Add(osmElement.Id, osmRelation);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(osmElement));
                }
            }
            
#if BENCHMARK
            stopwatch.Stop();
            Console.WriteLine("OSM data conversion took " + stopwatch.ElapsedMilliseconds + " ms");
            stopwatch.Restart();
#endif

            // Link and backlink all the elements together

            foreach (OsmWay osmWay in _waysById.Values)
            {
                Way rawWay = (Way)osmWay.RawElement;

                foreach (long rawWayId in rawWay.Nodes)
                {
                    OsmNode node = _nodesById[rawWayId];

                    // Link
                    osmWay.nodes.Add(node);

                    // Backlink
                    if (node.ways == null)
                        node.ways = new List<OsmNode>();

                    node.ways.Add(node);
                }
            }

            foreach (OsmRelation osmRelation in _relationsById.Values)
            {
                foreach (OsmRelationMember member in osmRelation.members)
                {
                    switch (member.ElementType)
                    {
                        case OsmRelationMember.MemberElementType.Node:
                            if (_nodesById.TryGetValue(member.Id, out OsmNode? node))
                            {
                                // Link
                                member.Element = node;
                                // Backlink
                                if (node.relations == null)
                                    node.relations = new List<OsmRelationMember>();
                                node.relations.Add(member);
                            }

                            break;

                        case OsmRelationMember.MemberElementType.Way:
                            if (_waysById.TryGetValue(member.Id, out OsmWay? way))
                            {
                                // Link
                                member.Element = way;
                                // Backlink
                                if (way.relations == null)
                                    way.relations = new List<OsmRelationMember>();
                                way.relations.Add(member);
                            }

                            break;

                        case OsmRelationMember.MemberElementType.Relation:
                            if (_relationsById.TryGetValue(member.Id, out OsmRelation? relation))
                            {
                                // Link
                                member.Element = relation;
                                // Backlink
                                if (relation.relations == null)
                                    relation.relations = new List<OsmRelationMember>();
                                relation.relations.Add(member);
                            }

                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

#if BENCHMARK
            stopwatch.Stop();
            Console.WriteLine("OSM data linking took " + stopwatch.ElapsedMilliseconds + " ms");
#endif
        }
    }
}