namespace Osmalyzer;

/// <summary>
/// For one or few values.
/// Value is expected.
/// </summary>
public class ValidateElementHasValue : ValidationRule
{
    public string Tag { get; }
    
    public string[] Values { get; }

    
    public ValidateElementHasValue(string tag, string value)
    {
        Tag = tag;
        Values = new[] { value };
    }
    
    public ValidateElementHasValue(string tag, params string[] values)
    {
        Tag = tag;
        Values = values;
    }
}