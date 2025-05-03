namespace Osmalyzer;

public class ValidateElementValueMatchesDataItemValue<T> : ValidationRule where T : IDataItem
{
    public string Tag { get; }
    
    public Func<T, string?> DataItemValueLookup { get; }


    public ValidateElementValueMatchesDataItemValue(string tag, Func<T, string?> dataItemValueLookup)
    {
        Tag = tag;
        DataItemValueLookup = dataItemValueLookup;
    }
}