//#if !REMOTE_EXECUTION
#define BENCHMARK
using System.Diagnostics;
//#endif

using System;
using System.IO;
using OsmSharp;
using OsmSharp.Streams;

namespace Osmalyzer;

/// <summary>
/// Full list of OSM data, as loaded.
/// </summary>
public class OsmMasterData : OsmData
{
    public OsmMasterData(string dataFileName)
    {
#if BENCHMARK
        // As of last benchmark:
        // OSMSharp data loading took 15822 ms
        // OSM data conversion took 3540 ms
        // OSM data linking took 4402 ms


        // At this point, I cannot think of any (non micro-) optimization to do here.
        // The bulk of the work is 15 sec for the PBF file reading and processing,
        // so the remaining 6.5 sec are largely irrelevant then.

        Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            
        // Read the "raw" elements from the file

        using FileStream fileStream = new FileInfo(dataFileName).OpenRead();

        using PBFOsmStreamSource source = new PBFOsmStreamSource(fileStream);

        List<OsmGeo> rawElements = new List<OsmGeo>();

        int nodeCount = 0;
        int wayCount = 0;
        int relationCount = 0;
            
        foreach (OsmGeo geo in source)
        {
            rawElements.Add(geo);

            switch (geo.Type)
            {
                case OsmGeoType.Node:     nodeCount++; break;
                case OsmGeoType.Way:      wayCount++; break;
                case OsmGeoType.Relation: relationCount++; break;
            }
        }

#if BENCHMARK
        stopwatch.Stop();
        Console.WriteLine("OSMSharp data loading took " + stopwatch.ElapsedMilliseconds + " ms");
        stopwatch.Restart();
#endif
            
        // Convert the "raw" elements to our own structure

        Dictionary<long, OsmNode> nodesById = new Dictionary<long, OsmNode>(nodeCount);
        Dictionary<long, OsmWay> waysById = new Dictionary<long, OsmWay>(wayCount);
        Dictionary<long, OsmRelation> relationsById = new Dictionary<long, OsmRelation>(relationCount);
            
        CreateElements(rawElements.Count, nodeCount, wayCount, relationCount);

        foreach (OsmGeo element in rawElements)
        {
            OsmElement osmElement = OsmElement.Create(element);

            AddElement(osmElement);

            switch (osmElement)
            {
                case OsmNode osmNode:
                    nodesById.Add(osmElement.Id, osmNode);
                    break;

                case OsmWay osmWay:
                    waysById.Add(osmElement.Id, osmWay);
                    break;
                    
                case OsmRelation osmRelation:
                    relationsById.Add(osmElement.Id, osmRelation);
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

        foreach (OsmWay osmWay in waysById.Values)
        {
            foreach (long rawWayId in osmWay.nodeIds)
            {
                OsmNode node = nodesById[rawWayId];

                // Link
                osmWay.nodes.Add(node);

                // Backlink
                if (node.ways == null)
                    node.ways = new List<OsmWay>();

                node.ways.Add(osmWay);
            }
        }

        foreach (OsmRelation osmRelation in relationsById.Values)
        {
            foreach (OsmRelationMember member in osmRelation.members)
            {
                switch (member.ElementType)
                {
                    case OsmRelationMember.MemberElementType.Node:
                        if (nodesById.TryGetValue(member.Id, out OsmNode? node))
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
                        if (waysById.TryGetValue(member.Id, out OsmWay? way))
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
                        if (relationsById.TryGetValue(member.Id, out OsmRelation? relation))
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