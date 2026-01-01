using OsmSharp;

namespace Osmalyzer;

public class OsmNode : OsmElement
{
    public override OsmElementType ElementType => OsmElementType.Node;

    public override string OsmViewUrl => "https://osm.org/node/" + Id;

    [PublicAPI]
    public readonly OsmCoord coord;
    
    [PublicAPI]
    public IReadOnlyList<OsmWay>? Ways => ways?.AsReadOnly();


    internal List<OsmWay>? ways;


    internal OsmNode(OsmGeo rawElement)
        : base(rawElement)
    {
        Node rawNode = (Node)rawElement;

        coord = new OsmCoord(
            rawNode.Latitude!.Value,
            rawNode.Longitude!.Value
        );
    }

    /// <summary>
    /// Copy constructor for deep copying nodes
    /// </summary>
    internal OsmNode(OsmNode original)
        : base(original)
    {
        coord = original.coord;
        // Note: ways backlink is NOT copied here - handled in OsmData.Copy()
    }


    public override OsmCoord AverageCoord => coord;
}