namespace Osmalyzer
{
    public class SortEntryAsc : EntrySortingRule
    {
        public int Value { get; }


        public SortEntryAsc(int value)
        {
            Value = value;
        }
    }
}