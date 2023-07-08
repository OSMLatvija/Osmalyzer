namespace Osmalyzer
{
    public class SortEntryAsc : EntrySortingRule
    {
        public int Value { get; }


        public SortEntryAsc(int value)
        {
            Value = value;
        }
        
        public SortEntryAsc(object value)
        {
            Value = (int)value; // we will fail of course if not int or castable to int, but that's by design
        }
    }
}