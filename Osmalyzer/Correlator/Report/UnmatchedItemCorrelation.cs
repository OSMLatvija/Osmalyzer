namespace Osmalyzer;

public class UnmatchedItemCorrelation<T> : Correlation where T : IDataItem
{
    public T DataItem { get; set; }

    
    public UnmatchedItemCorrelation(T dataItem)
    {
        DataItem = dataItem;
    }
}