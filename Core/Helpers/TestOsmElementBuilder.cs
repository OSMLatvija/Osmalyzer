using System.Linq;
using OsmSharp;

namespace Osmalyzer;

public static class TestOsmElementBuilder
{
    private static long _nextId = 1;

    public static OsmNode CreateNode()
    {
        long id = _nextId++;

        Node raw = new Node
        {
            Id = id,
            Version = 42,
            ChangeSetId = 1337,
            Latitude = id * 1e-6 + 56.0, // deterministic but irrelevant
            Longitude = id * 1e-6 + 24.0,
            Tags = null
        };

        return (OsmNode)OsmElement.Create(raw);
    }

    public static OsmWay CreateWay(params OsmNode[] nodes)
    {
        long id = _nextId++;

        Way raw = new Way
        {
            Id = id,
            Version = 42,
            ChangeSetId = 1337,
            Nodes = nodes.Select(n => n.Id).ToArray(),
            Tags = null
        };

        OsmWay way = (OsmWay)OsmElement.Create(raw);

        // Link objects
        way.nodes.AddRange(nodes);
        foreach (OsmNode node in nodes)
        {
            node.ways ??= new List<OsmWay>();
            if (!node.ways.Contains(way))
                node.ways.Add(way);
        }

        return way;
    }

    public static OsmRelation CreateRelation(params (OsmElement element, string role)[] members)
    {
        long id = _nextId++;

        RelationMember[] rawMembers = members.Select(m => new RelationMember()
        {
            Type = m.element switch
            {
                OsmNode => OsmGeoType.Node,
                OsmWay => OsmGeoType.Way,
                OsmRelation => OsmGeoType.Relation,
                _ => OsmGeoType.Node // default, shouldn't happen
            },
            Id = m.element.Id,
            Role = m.role
        }).ToArray();

        Relation raw = new Relation
        {
            Id = id,
            Version = 42,
            ChangeSetId = 1337,
            Members = rawMembers,
            Tags = null
        };

        OsmRelation rel = (OsmRelation)OsmElement.Create(raw);

        // Link the created members to actual elements (so tests can use Element references)
        for (int i = 0; i < rel.Members.Count; i++)
        {
            if (i < members.Length)
            {
                rel.Members[i].Element = members[i].element;
            }
        }

        return rel;
    }
}
