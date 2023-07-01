using System;

namespace Osmalyzer
{
    public class SortEntryDesc : EntrySortingRule
    {
        public int Value { get; }


        public SortEntryDesc(int value)
        {
            Value = value;
        }
    }
}