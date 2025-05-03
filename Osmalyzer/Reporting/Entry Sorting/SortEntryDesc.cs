namespace Osmalyzer;

public class SortEntryDesc : EntrySortingRule
{
    public IComparable Value { get; }


    public SortEntryDesc(IComparable value)
    {
        Value = value;
    }
}