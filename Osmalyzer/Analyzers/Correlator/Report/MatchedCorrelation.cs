namespace Osmalyzer;

public class MatchedCorrelation<T> : Correlation where T : ICorrelatorItem
{
    public OsmElement OsmElement { get; set; }

    public T DataItem { get; set; }
    
    public double Distance { get; }


    public MatchedCorrelation(OsmElement osmElement, T dataItem, double distance)
    {
        OsmElement = osmElement;
        DataItem = dataItem;
        Distance = distance;
    }
}