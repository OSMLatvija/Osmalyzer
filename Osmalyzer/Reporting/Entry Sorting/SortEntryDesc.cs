namespace Osmalyzer
{
    public class SortEntryDesc : EntrySortingRule
    {
        public int Value { get; }


        public SortEntryDesc(int value)
        {
            Value = value;
        }
        
        public SortEntryDesc(object value)
        {
            Value = (int)value; // we will fail of course if not int or castable to int, but that's by design
        }
    }
}