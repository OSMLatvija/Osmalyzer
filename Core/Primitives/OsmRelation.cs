using System.Linq;
using OsmSharp;

namespace Osmalyzer;

public class OsmRelation : OsmElement
{
    public override OsmElementType ElementType => OsmElementType.Relation;

    public override string OsmViewUrl => "https://osm.org/relation/" + Id;

    [PublicAPI]
    public IReadOnlyList<OsmRelationMember> Members => members.AsReadOnly();

    /// <summary>
    ///
    /// This will not contain null/missing elements, even if some are not loaded.
    /// </summary>
    [PublicAPI]
    public IEnumerable<OsmElement> Elements => members.Where(m => m.Element != null).Select(m => m.Element)!;
        
        
    internal readonly List<OsmRelationMember> members;
    
    
    private OsmCoord? _cachedAverageCoord;


    internal OsmRelation(OsmGeo rawElement, OsmData owner)
        : base(rawElement, owner)
    {
        members = ((Relation)rawElement).Members.Select(m => new OsmRelationMember(this, m.Type, m.Id, m.Role)).ToList();
    }

    /// <summary>
    /// Copy constructor for deep copying relations
    /// </summary>
    internal OsmRelation(OsmRelation original)
        : base(original)
    {
        // Deep copy members but without linking elements yet (that's done in OsmData.Copy())
        members = original.members.Select(m => new OsmRelationMember(this, m.ElementType, m.Id, m.Role)).ToList();
        // Note: member elements and relations backlink are NOT copied here - handled in OsmData.Copy()
    }
        
    [Pure]
    public OsmPolygon? GetOuterWayPolygon()
    {
        List<OsmWay> outerWays = GetOuterWays();

        List<OsmWay>? sortedWays = OsmAlgorithms.SortWays(outerWays);

        if (sortedWays == null)
            return null; // invalid geo
        
        List<OsmNode> nodes = OsmAlgorithms.CollectNodes(sortedWays);

        return new OsmPolygon(nodes.Select(n => n.coord).ToList());
    }

    [Pure]
    public OsmMultiPolygon? GetMultipolygon()
    {
        List<OsmPolygon> outerRings = [ ];
        List<OsmPolygon> innerRings = [ ];

        // Group ways by role
        List<OsmWay> outerWays = [ ];
        List<OsmWay> innerWays = [ ];

        foreach (OsmRelationMember member in Members)
        {
            if (member.Element is OsmWay wayElement)
            {
                if (member.Role == "outer")
                    outerWays.Add(wayElement);
                else if (member.Role == "inner")
                    innerWays.Add(wayElement);
            }
        }

        // Process outer ways - find all disconnected rings
        if (outerWays.Count > 0)
        {
            List<List<OsmWay>> outerRingGroups = GroupConnectedWays(outerWays);
            
            foreach (List<OsmWay> ringGroup in outerRingGroups)
            {
                List<OsmWay>? sortedWays = OsmAlgorithms.SortWays(ringGroup);
                if (sortedWays == null)
                    continue; // skip invalid rings, don't fail entire multipolygon
                
                List<OsmNode> nodes = OsmAlgorithms.CollectNodes(sortedWays);
                if (nodes.Count >= 3) // need at least 3 points for a valid polygon
                    outerRings.Add(new OsmPolygon(nodes.Select(n => n.coord).ToList()));
            }
        }

        // Process inner ways - find all disconnected rings
        if (innerWays.Count > 0)
        {
            List<List<OsmWay>> innerRingGroups = GroupConnectedWays(innerWays);
            
            foreach (List<OsmWay> ringGroup in innerRingGroups)
            {
                List<OsmWay>? sortedWays = OsmAlgorithms.SortWays(ringGroup);
                if (sortedWays == null)
                    continue; // skip invalid rings
                
                List<OsmNode> nodes = OsmAlgorithms.CollectNodes(sortedWays);
                if (nodes.Count >= 3) // need at least 3 points for a valid polygon
                    innerRings.Add(new OsmPolygon(nodes.Select(n => n.coord).ToList()));
            }
        }

        if (outerRings.Count == 0)
            return null; // no valid outer ring

        return new OsmMultiPolygon(outerRings, innerRings);
    }

    /// <summary>
    /// Groups ways into connected rings. Ways that share endpoints form a connected ring.
    /// </summary>
    private static List<List<OsmWay>> GroupConnectedWays(List<OsmWay> ways)
    {
        List<List<OsmWay>> groups = [ ];
        HashSet<OsmWay> unprocessed = new HashSet<OsmWay>(ways);

        while (unprocessed.Count > 0)
        {
            List<OsmWay> currentGroup = [ ];
            Queue<OsmWay> toProcess = new Queue<OsmWay>();
            
            // Start with first unprocessed way
            OsmWay firstWay = unprocessed.First();
            toProcess.Enqueue(firstWay);
            unprocessed.Remove(firstWay);

            // Find all ways connected to this group
            while (toProcess.Count > 0)
            {
                OsmWay currentWay = toProcess.Dequeue();
                currentGroup.Add(currentWay);

                // Get endpoints of current way
                if (currentWay.nodes.Count < 2)
                    continue;

                long firstNodeId = currentWay.nodes[0].Id;
                long lastNodeId = currentWay.nodes[currentWay.nodes.Count - 1].Id;

                // Find all unprocessed ways that connect to this way
                List<OsmWay> connected = [ ];
                foreach (OsmWay way in unprocessed)
                {
                    if (way.nodes.Count < 2)
                        continue;

                    long wayFirstId = way.nodes[0].Id;
                    long wayLastId = way.nodes[way.nodes.Count - 1].Id;

                    // Check if this way connects to current way
                    if (wayFirstId == firstNodeId || wayFirstId == lastNodeId ||
                        wayLastId == firstNodeId || wayLastId == lastNodeId)
                    {
                        connected.Add(way);
                    }
                }

                // Add connected ways to processing queue
                foreach (OsmWay way in connected)
                {
                    toProcess.Enqueue(way);
                    unprocessed.Remove(way);
                }
            }

            if (currentGroup.Count > 0)
                groups.Add(currentGroup);
        }

        return groups;
    }

    [Pure]
    public List<OsmWay> GetOuterWays()
    {
        List<OsmWay> outerWays = [ ];

        foreach (OsmRelationMember member in Members)
        {
            if (member.Element is OsmWay wayElement && member.Role == "outer")
            {
                outerWays.Add(wayElement);
            }
        }

        return outerWays;
    }

    public override OsmCoord AverageCoord
    {
        [Pure] get { return _cachedAverageCoord ??= OsmGeoTools.GetAverageCoord(Elements); }
    }

    [Pure]
    public IEnumerable<T> GetElementsWithRole<T>(string role) where T : OsmElement
    {
        return Members.Where(m => m.Element is T && m.Role == role).Select(m => (T)m.Element!);
    }
}