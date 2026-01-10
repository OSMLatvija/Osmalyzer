using System.Linq;
using OsmSharp;

namespace Osmalyzer;

public class OsmWay : OsmElement
{
    public override OsmElementType ElementType => OsmElementType.Way;

    public override string OsmViewUrl => "https://osm.org/way/" + Id;

    /// <summary>
    /// Note: closed ways will repeat the last/first node.
    /// This cannot be 0 nodes (normally) as that isn't a valid OSM object.
    /// </summary>
    [PublicAPI]
    public IReadOnlyList<OsmNode> Nodes => nodes.AsReadOnly();

    public bool Closed => nodes.Count >= 3 && nodes[0] == nodes[^1];


    /// <summary>
    /// The coord of this element, depending on type. Exact coord for nodes, average coord for ways and relations.
    /// This is cached on first access, so it's fast to read again.
    /// </summary>
    public override OsmCoord AverageCoord => _cachedAverageCoord ??= OsmGeoTools.GetAverageCoord(nodes);


    internal readonly List<OsmNode> nodes = [ ];
        
    internal readonly long[] nodeIds;

    
    private OsmCoord? _cachedAverageCoord;


    internal OsmWay(OsmGeo rawElement, OsmData owner)
        : base(rawElement, owner)
    {
        nodeIds = ((Way)rawElement).Nodes;
    }

    /// <summary>
    /// Copy constructor for deep copying ways
    /// </summary>
    internal OsmWay(OsmWay original)
        : base(original)
    {
        nodeIds = original.nodeIds;
        // Note: nodes list and relations backlink are NOT copied here - handled in OsmData.Copy()
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