using System.Collections.Generic;
using JetBrains.Annotations;
using OsmSharp;

namespace Osmalyzer;

public class OsmNode : OsmElement
{
    public override OsmElementType ElementType => OsmElementType.Node;

    public override string OsmViewUrl => "https://osm.org/node/" + Id;

    [PublicAPI]
    public readonly OsmCoord coord;
    [PublicAPI]
    public IReadOnlyList<OsmNode>? Ways => ways?.AsReadOnly();


    internal List<OsmNode>? ways;


    internal OsmNode(OsmGeo rawElement)
        : base(rawElement)
    {
        Node rawNode = (Node)rawElement;

        coord = new OsmCoord(
            rawNode.Latitude!.Value,
            rawNode.Longitude!.Value
        );
    }
        
        
    public override OsmCoord GetAverageCoord()
    {
        return coord;
    }
}