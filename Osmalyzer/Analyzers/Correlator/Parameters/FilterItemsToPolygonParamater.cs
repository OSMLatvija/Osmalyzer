namespace Osmalyzer;

public class FilterItemsToPolygonParamater : CorrelatorParamater
{
    public OsmPolygon Polygon { get; }

    
    public FilterItemsToPolygonParamater(OsmPolygon polygon)
    {
        Polygon = polygon;
    }
}