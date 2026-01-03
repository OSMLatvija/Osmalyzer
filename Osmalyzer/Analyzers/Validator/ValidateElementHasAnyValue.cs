namespace Osmalyzer;

/// <summary>
/// For one or few values.
/// Value is expected.
/// </summary>
public class ValidateElementHasAnyValue : ValidationRule
{
    public string Tag { get; }

    public string[] Values { get; }
    

    public ValidateElementHasAnyValue(string tag, params string[] values)
    {
        Tag = tag;
        Values = values;
    }
}