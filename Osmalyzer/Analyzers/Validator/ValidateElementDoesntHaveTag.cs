namespace Osmalyzer;

/// <summary>
/// Value is not expected.
/// </summary>
public class ValidateElementDoesntHaveTag : ValidationRule
{
    public string Tag { get; }

    
    public ValidateElementDoesntHaveTag(string tag)
    {
        Tag = tag;
    }
}