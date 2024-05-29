namespace Osmalyzer;

/// <summary>
/// Value is not expected.
/// </summary>
public class ValidateElementDoesntHaveValue : ValidationRule
{
    public string Tag { get; }

    
    public ValidateElementDoesntHaveValue(string tag)
    {
        Tag = tag;
    }
}