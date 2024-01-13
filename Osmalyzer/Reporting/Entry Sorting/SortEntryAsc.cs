using System;

namespace Osmalyzer;

public class SortEntryAsc : EntrySortingRule
{
    public IComparable Value { get; }


    public SortEntryAsc(IComparable value)
    {
        Value = value;
    }
}