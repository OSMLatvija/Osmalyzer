namespace Osmalyzer;

public class ValidateElementValueMatchesDataItemValue<T> : ValidationRule where T : IDataItem
{
    // todo: allow reporting issue if the value is not provided, i.e. we expect value but we don't know what it should be based on data
    
    public Func<OsmElement, bool>? ShouldCheckElement { get; }
    
    public Func<OsmElement, OsmElement>? ElementSelector { get; }

    public string Tag { get; }

    public Func<T, string?> DataItemValueLookup { get; }
    
    public string[]? IncorrectTags { get; }


    public ValidateElementValueMatchesDataItemValue(Func<OsmElement, bool> shouldCheckElement, string tag, Func<T, string?> dataItemValueLookup)
    {
        ShouldCheckElement = shouldCheckElement;
        Tag = tag;
        DataItemValueLookup = dataItemValueLookup;
    }

    public ValidateElementValueMatchesDataItemValue(Func<OsmElement, bool> shouldCheckElement, Func<OsmElement, OsmElement> elementSelector, string tag, Func<T, string?> dataItemValueLookup)
    {
        ShouldCheckElement = shouldCheckElement;
        ElementSelector = elementSelector;
        Tag = tag;
        DataItemValueLookup = dataItemValueLookup;
    }

    public ValidateElementValueMatchesDataItemValue(string tag, Func<T, string?> dataItemValueLookup, string[]? incorrectTags = null)
    {
        Tag = tag;
        DataItemValueLookup = dataItemValueLookup;
        IncorrectTags = incorrectTags;
    }
}