using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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


    internal OsmRelation(OsmGeo rawElement)
        : base(rawElement)
    {
        members = ((Relation)rawElement).Members.Select(m => new OsmRelationMember(this, m.Type, m.Id, m.Role)).ToList();
    }

        
    [Pure]
    public OsmPolygon GetOuterWayPolygon()
    {
        List<OsmWay> outerWays = GetOuterWays();

        outerWays = OsmAlgorithms.SortWays(outerWays);

        List<OsmNode> nodes = OsmAlgorithms.CollectNodes(outerWays);

        return new OsmPolygon(nodes.Select(n => n.coord).ToList());
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

    [Pure]
    public override OsmCoord GetAverageCoord()
    {
        return _cachedAverageCoord ??= OsmGeoTools.GetAverageCoord(Elements);
    }

    [Pure]
    public IEnumerable<T> GetElementsWithRole<T>(string role) where T : OsmElement
    {
        return Members.Where(m => m.Element is T && m.Role == role).Select(m => (T)m.Element!);
    }
}