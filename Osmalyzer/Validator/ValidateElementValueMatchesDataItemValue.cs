namespace Osmalyzer;

public class ValidateElementValueMatchesDataItemValue<T> : ValidationRule where T : IDataItem
{
    // todo: allow reporting issue if the value is not provided, i.e. we expect value but we don't know what it should be based on data
    
    public Func<OsmElement, T, bool>? ShouldCheckElement { get; }
    
    public Func<OsmElement, OsmElement>? ElementSelector { get; }

    public string Tag { get; }

    /// <summary>
    /// Expected value, if given. If empty, expecting no value. If null, don't know what is expected.
    /// </summary>
    public Func<T, string?> DataItemValueLookup { get; }

    public bool AllowAsMultiValue { get; }

    public string[]? IncorrectTags { get; }


    public ValidateElementValueMatchesDataItemValue(Func<OsmElement, T, bool> shouldCheckElement, string tag, Func<T, string?> dataItemValueLookup, bool allowAsMultiValue = false)
    {
        ShouldCheckElement = shouldCheckElement;
        Tag = tag;
        DataItemValueLookup = dataItemValueLookup;
        AllowAsMultiValue = allowAsMultiValue;
    }

    public ValidateElementValueMatchesDataItemValue(Func<OsmElement, T, bool> shouldCheckElement, Func<OsmElement, OsmElement> elementSelector, string tag, Func<T, string?> dataItemValueLookup)
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