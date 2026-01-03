namespace Osmalyzer;

/// <summary>
/// For one or few values.
/// Value is expected.
/// </summary>
public class ValidateElementHasValue : ValidationRule
{
    public string Tag { get; }

    /// <summary>
    /// A value set means that specific value is expected.
    /// Empty means value is expected to NOT be set.
    /// Null means we don't know what value if any is expected.
    /// </summary>
    public string? Value { get; }

    public string[]? IncorrectTags { get; }
    
    
    public ValidateElementHasValue(string tag, string? value, string[]? incorrectTags = null)
    {
        Tag = tag;
        Value = value;
        IncorrectTags = incorrectTags;
    }
}