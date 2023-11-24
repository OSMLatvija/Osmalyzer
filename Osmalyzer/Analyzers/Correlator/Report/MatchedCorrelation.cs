namespace Osmalyzer;

public class MatchedCorrelation<T> : Correlation where T : ICorrelatorItem
{
    public OsmElement OsmElement { get; set; }

    public T DataItem { get; set; }

    
    public MatchedCorrelation(OsmElement osmElement, T dataItem)
    {
        OsmElement = osmElement;
        DataItem = dataItem;
    }
}