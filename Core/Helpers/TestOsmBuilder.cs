using System.Collections.Generic;
using System.Linq;
using OsmSharp;

namespace Osmalyzer;

public static class TestOsmBuilder
{
    public static OsmNode Node(long id)
    {
        Node raw = new Node
        {
            Id = id,
            Latitude = id * 1e-6 + 56.0, // deterministic but irrelevant
            Longitude = id * 1e-6 + 24.0,
            Tags = null
        };

        return (OsmNode)OsmElement.Create(raw);
    }

    public static OsmWay Way(long id, params OsmNode[] nodes)
    {
        Way raw = new Way
        {
            Id = id,
            Nodes = nodes.Select(n => n.Id).ToArray(),
            Tags = null
        };

        OsmWay way = (OsmWay)OsmElement.Create(raw);

        // Link objects (internal fields are accessible within Core)
        way.nodes.AddRange(nodes);
        foreach (OsmNode node in nodes)
        {
            node.ways ??= new List<OsmWay>();
            if (!node.ways.Contains(way))
                node.ways.Add(way);
        }

        return way;
    }

    public static OsmRelation Relation(long id, params (OsmGeoType type, long refId, string role)[] members)
    {
        RelationMember[] rawMembers = members.Select(m => new RelationMember()
        {
            Type = m.type,
            Id = m.refId,
            Role = m.role
        }).ToArray();

        Relation raw = new Relation
        {
            Id = id,
            Members = rawMembers,
            Tags = null
        };

        return (OsmRelation)OsmElement.Create(raw);
    }
}

