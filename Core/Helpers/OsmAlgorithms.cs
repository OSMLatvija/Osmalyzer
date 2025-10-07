using System;
using System.Linq;

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

    [Pure]
    public static bool IsChained(OsmElement headElement, OsmElement[] midElements, OsmElement tailElement)
    {
        OsmElement[] elements = new OsmElement[2 + midElements.Length];
        elements[0] = headElement;
        Array.Copy(midElements, 0, elements, 1, midElements.Length);
        elements[^1] = tailElement;
        return IsChained(elements);
    }

    [Pure]
    public static bool IsChained(params OsmElement[] elements)
    {
        List<OsmElement> chain = elements.ToList();

        if (chain.Count == 0)
            return false; // empty is invalid

        // Relations are not valid in the chain at all
        if (chain.Any(e => e is OsmRelation))
            return false;

        if (chain.Count == 1)
            return chain[0] is not OsmRelation; // single non-relation element is trivially chained

        // Validate adjacency
        for (int i = 0; i < chain.Count - 1; i++)
        {
            OsmElement a = chain[i];
            OsmElement b = chain[i + 1];

            switch (a)
            {
                case OsmWay wa when b is OsmWay wb:
                {
                    // Ways must connect via terminal nodes only
                    if (!WaysShareTerminalNode(wa, wb))
                        return false;
                    break;
                }

                case OsmWay wa when b is OsmNode nb:
                {
                    // Node must be a terminal node of the way
                    if (!IsTerminalNodeOf(wa, nb))
                        return false;
                    break;
                }

                case OsmNode na when b is OsmWay wb:
                {
                    // Node must be a terminal node of the way
                    if (!IsTerminalNodeOf(wb, na))
                        return false;
                    break;
                }

                case OsmNode:
                    // Adjacent nodes are never a valid chain segment
                    return false;

                default:
                    // Unknown element type encountered
                    return false;
            }
        }

        return true;

        // Local helpers
        static bool IsTerminalNodeOf(OsmWay way, OsmNode node)
        {
            IReadOnlyList<OsmNode> nodes = way.Nodes;
            if (nodes.Count < 1) return false;
            OsmNode first = nodes[0];
            OsmNode last = nodes[^1];
            return node == first || node == last;
        }

        static bool WaysShareTerminalNode(OsmWay a, OsmWay b)
        {
            IReadOnlyList<OsmNode> an = a.Nodes;
            IReadOnlyList<OsmNode> bn = b.Nodes;
            if (an.Count < 1 || bn.Count < 1) return false;

            OsmNode aFirst = an[0];
            OsmNode aLast = an[^1];
            OsmNode bFirst = bn[0];
            OsmNode bLast = bn[^1];

            return aFirst == bFirst || aFirst == bLast || aLast == bFirst || aLast == bLast;
        }
    }
}