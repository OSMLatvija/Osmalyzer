namespace Osmalyzer;

public static class FuzzyAddressParser
{
    /// <summary>
    /// Parse freeform text address into components.
    /// </summary>
    [Pure]
    public static bool TryParseAddress(string raw, out string? streetLine, out string? city, out string? postalCode)
    {
        // TODO: ACTUAL
        
        streetLine = null;
        city = null;
        postalCode = null;
        
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        
        string[] parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            return false;
        
        streetLine = parts[0];
        city = parts[1];
        postalCode = parts[2];
        
        // Remove spaces within postal code
        postalCode = postalCode.Replace(" ", "");

        return true;
    }
}