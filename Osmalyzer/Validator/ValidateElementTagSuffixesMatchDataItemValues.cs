namespace Osmalyzer;

/// <summary>
/// Validates that for each value in the data item's list, a tag of the form "prefix:value=yes" is present on the element,
/// and that no extra "prefix:*=yes" tags exist beyond those in the list.
/// For example, with prefix "language" and values ["lv", "en"], expects "language:lv=yes" and "language:en=yes".
/// </summary>
public class ValidateElementTagSuffixesMatchDataItemValues<T> : ValidationRule where T : IDataItem
{
    /// <summary>Tag prefix, e.g. "language"</summary>
    public string TagPrefix { get; }

    /// <summary>Expected value for each suffixed tag, e.g. "yes"</summary>
    public string ExpectedValue { get; }

    public Func<T, List<string>?> DataItemValuesLookup { get; }


    public ValidateElementTagSuffixesMatchDataItemValues(string tagPrefix, string expectedValue, Func<T, List<string>?> dataItemValuesLookup)
    {
        TagPrefix = tagPrefix;
        ExpectedValue = expectedValue;
        DataItemValuesLookup = dataItemValuesLookup;
    }
}

