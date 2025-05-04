using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OsmSharp;

namespace Osmalyzer;

public class OsmWay : OsmElement
{
    public override OsmElementType ElementType => OsmElementType.Way;

    public override string OsmViewUrl => "https://osm.org/way/" + Id;

    /// <summary>
    /// Note: closed ways will repeat the last/first node.
    /// </summary>
    [PublicAPI]
    public IReadOnlyList<OsmNode> Nodes => nodes.AsReadOnly();

    public bool Closed => nodes.Count >= 3 && nodes[0] == nodes[^1];


    internal readonly List<OsmNode> nodes = new List<OsmNode>();
        
    internal readonly long[] nodeIds;

    
    private OsmCoord? _cachedAverageCoord;


    internal OsmWay(OsmGeo rawElement)
        : base(rawElement)
    {
        nodeIds = ((Way)rawElement).Nodes;
    }

        
    public override OsmCoord GetAverageCoord()
    {
        return _cachedAverageCoord ??= OsmGeoTools.GetAverageCoord(nodes);
    }
    
    [Pure]
    public OsmPolygon GetPolygon()
    {
        return new OsmPolygon(nodes.Select(n => n.coord).ToList());
    }

    [Pure]
    public bool ContainsCoord(OsmCoord coord)
    {
        return GetPolygon().ContainsCoord(coord);
    }
}