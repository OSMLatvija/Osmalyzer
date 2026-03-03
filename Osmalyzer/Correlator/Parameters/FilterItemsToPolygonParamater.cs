namespace Osmalyzer;

public class FilterItemsToPolygonParamater : CorrelatorParamater
{
    public OsmPolygon Polygon { get; }
    
    public bool Report { get; }


    public FilterItemsToPolygonParamater(OsmPolygon polygon, bool report)
    {
        Polygon = polygon;
        Report = report;
    }
}