namespace Osmalyzer;

public class ValidateElementValueMatchesDataItemValue<T> : ValidationRule where T : IDataItem
{
    public string Tag { get; }
    
    public Func<T, string?> DataItemValueLookup { get; }
    
    public string[]? IncorrectTags { get; }


    public ValidateElementValueMatchesDataItemValue(string tag, Func<T, string?> dataItemValueLookup, string[]? incorrectTags = null)
    {
        Tag = tag;
        DataItemValueLookup = dataItemValueLookup;
        IncorrectTags = incorrectTags;
    }
}