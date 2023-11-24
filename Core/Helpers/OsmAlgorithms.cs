using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

public static class OsmAlgorithms
{
    /// <summary>
    /// Assuming the ways form a correct multi-polygon, return them in a sorted order.
    /// </summary>
    [Pure]
    public static List<OsmWay> SortWays(List<OsmWay> ways)
    {
        // We will need to look up ways from terminal nodes, so pre-make such lists
        // (otherwise, it can be extremely slow)
            
        Dictionary<OsmNode, OsmWay> nodes1 = new Dictionary<OsmNode, OsmWay>();
        Dictionary<OsmNode, OsmWay> nodes2 = new Dictionary<OsmNode, OsmWay>();

        foreach (OsmWay osmWay in ways)
        {
            if (osmWay.nodes.Count < 2) throw new InvalidOperationException();
                
            OsmNode first = osmWay.nodes.First();
            OsmNode last = osmWay.nodes.Last();

            if (!nodes1.ContainsKey(first))
                nodes1.Add(first, osmWay);
            else if (!nodes2.ContainsKey(first))
                nodes2.Add(first, osmWay);
            else
                throw new InvalidOperationException();

            if (!nodes1.ContainsKey(last))
                nodes1.Add(last, osmWay);
            else if (!nodes2.ContainsKey(last))
                nodes2.Add(last, osmWay);
            else
                throw new InvalidOperationException();
        }

        // Now build the way "circle" based on their terminal nodes
            
        List<OsmWay> sortedWays = new List<OsmWay>(ways.Count);
            
        OsmWay way = ways[0]; // any way will do
        OsmNode node = way.nodes[0]; // any way's start/end node will do
        sortedWays.Add(way);

        for (int i = 0; i < ways.Count - 1; i++) // one less because we manually picked one (we don't actually care about this index)
        {
            OsmNode first = way.nodes.First();
            OsmNode last = way.nodes.Last();

            // Next node has to be the last of this way (which is the "other end" of whichever node we have)
            node = node == first ? last : first;

            OsmWay way1 = nodes1[node];
            OsmWay way2 = nodes2[node];

            // Each node has two ways where it's a terminal node, so pick whichever way we haven't add yet
            way = way == way1 ? way2 : way1;
                
            sortedWays.Add(way);
        }

        return sortedWays;
    }

    /// <summary>
    /// Assuming a sorted way order from something like a correct multi-polygon, return the nodes that make up the polygon.
    /// </summary>
    public static List<OsmNode> CollectNodes(List<OsmWay> ways)
    {
        List<OsmNode> nodes = new List<OsmNode>();

        OsmNode? lastNode = null;
            
        for (int i = 0; i < ways.Count; i++)
        {
            OsmWay way = ways[i];
                
            bool reverse = lastNode != null && lastNode != way.nodes[0];

            if (!reverse)
            {
                for (int n = 0; n < way.nodes.Count - 1; n++) // excluding last
                    nodes.Add(way.nodes[n]);

                lastNode = way.nodes[^1];
            }
            else
            {
                for (int n = way.nodes.Count - 1; n > 0; n--) // excluding last (i.e. first in way list)
                    nodes.Add(way.nodes[n]);
                    
                lastNode = way.nodes[0];
            }
        }

        return nodes;
    }
}