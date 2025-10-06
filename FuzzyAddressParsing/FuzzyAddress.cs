namespace Osmalyzer;

/// <summary>
/// Parsed fuzzy address from <see cref="FuzzyAddressParser"/>.
/// </summary>
public record FuzzyAddress
{
    public List<FuzzyAddressPart> Parts { get; }
    
    
    public FuzzyAddress(List<FuzzyAddressPart> parts)
    {
        if (parts == null) throw new ArgumentNullException(nameof(parts));
        if (parts.Count == 0) throw new ArgumentException("Must have at least one part.", nameof(parts));
        
        
        Parts = parts;
    }
}

