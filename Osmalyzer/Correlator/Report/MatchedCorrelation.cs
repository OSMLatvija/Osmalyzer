namespace Osmalyzer;

public class MatchedCorrelation<T> : Correlation where T : IDataItem
{
    public OsmElement OsmElement { get; set; }

    public T DataItem { get; set; }
    
    public double Distance { get; }
    
    public bool Far { get; }


    public MatchedCorrelation(OsmElement osmElement, T dataItem, double distance, bool far)
    {
        OsmElement = osmElement;
        DataItem = dataItem;
        Distance = distance;
        Far = far;
    }
}