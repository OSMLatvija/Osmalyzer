namespace Osmalyzer;

/// <summary>
/// Value is not expected.
/// </summary>
public class ValidateElementDoesntHaveTag : ValidationRule
{
    public Func<OsmElement, bool>? ShouldCheckElement { get; }
    
    public string Tag { get; }

    
    public ValidateElementDoesntHaveTag(Func<OsmElement, bool> shouldCheckElement, string tag)
    {
        ShouldCheckElement = shouldCheckElement;
        Tag = tag;
    }
    
    public ValidateElementDoesntHaveTag(string tag)
    {
        Tag = tag;
    }
}