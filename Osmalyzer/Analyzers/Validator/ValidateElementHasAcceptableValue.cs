namespace Osmalyzer;

/// <summary>
/// For elements with this value, but not required.
/// Value has to pass the check.
/// </summary>
public class ValidateElementHasAcceptableValue : ValidationRule
{
    public string Tag { get; }
    
    public Func<string, bool> Check { get; }

    public string ValueLabel { get; init; } = "valid value";


    public ValidateElementHasAcceptableValue(string tag, Func<string, bool> check, string? valueLabel = null)
    {
        Tag = tag;
        Check = check;
        if (valueLabel != null) ValueLabel = valueLabel;
    }
}