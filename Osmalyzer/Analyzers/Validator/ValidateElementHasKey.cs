namespace Osmalyzer;

/// <summary>
/// Value is expected, but not specific one.
/// </summary>
public class ValidateElementHasKey : ValidationRule
{
    public string Tag { get; }

    
    public ValidateElementHasKey(string tag)
    {
        Tag = tag;
    }
}