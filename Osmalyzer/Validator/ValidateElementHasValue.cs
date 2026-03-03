namespace Osmalyzer;

/// <summary>
/// For one or few values.
/// Value is expected.
/// </summary>
public class ValidateElementHasValue : ValidationRule
{
    public Func<OsmElement, bool>? ShouldCheckElement { get; }
    
    public Func<OsmElement, OsmElement>? ElementSelector { get; }

    public string Tag { get; }

    /// <summary>
    /// A value set means that specific value is expected.
    /// Empty means value is expected to NOT be set.
    /// Null means we don't know what value if any is expected.
    /// </summary>
    public string? Value { get; }

    public string[]? IncorrectTags { get; }


    public ValidateElementHasValue(Func<OsmElement, bool> shouldCheckElement, Func<OsmElement, OsmElement> elementSelector, string tag, string? value, string[]? incorrectTags = null)
    {
        ShouldCheckElement = shouldCheckElement;
        ElementSelector = elementSelector;
        Tag = tag;
        Value = value;
        IncorrectTags = incorrectTags;
    }

    public ValidateElementHasValue(Func<OsmElement, bool> shouldCheckElement, string tag, string? value, string[]? incorrectTags = null)
    {
        ShouldCheckElement = shouldCheckElement;
        Tag = tag;
        Value = value;
        IncorrectTags = incorrectTags;
    }

    public ValidateElementHasValue(string tag, string? value, string[]? incorrectTags = null)
    {
        Tag = tag;
        Value = value;
        IncorrectTags = incorrectTags;
    }
}