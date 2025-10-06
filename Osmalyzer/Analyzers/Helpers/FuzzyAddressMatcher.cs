namespace Osmalyzer;

public static class FuzzyAddressMatcher
{
    // TODO: rewrite to use tsv file from data
    private static readonly string[] _suffixes = 
    {
        "iela",
        "bulvāris",
        "ceļš",
        "gatve",
        "šoseja",
        "tilts",
        "dambis",
        "aleja",
        "apvedceļš",
        "laukums",
        "prospekts",
        "pārvads",
        "līnija",
        "šķērslīnija",
        "krastmala",
    };
    // Note: ImproperTranslationAnalyzer is doing Russian translations, so add value there if adding here
    
    
    [Pure]
    public static bool Matches(OsmElement element, string fuzzyAddress)
    {
        return Matches(
            element.GetValue("addr:street"),
            element.GetValue("addr:housenumber"),
            element.GetValue("addr:unit"),
            fuzzyAddress
        );
    }
    
    [Pure]
    public static bool Matches(string? tagStreet, string? tagHouseNumber, string fuzzyAddress)
    {
        return Matches(tagStreet, tagHouseNumber, null, fuzzyAddress);
    }

    [Pure]
    public static bool Matches(string? tagStreet, string? tagHouseNumber, string? tagUnit, string fuzzyAddress)
    {
        fuzzyAddress = fuzzyAddress.Trim().ToLower();
        
        if (fuzzyAddress == "")
            return false;
        
        // Street
        
        if (tagStreet == null)
            return false;

        tagStreet = tagStreet.ToLower();

        // Addresses often do "Ozolu 9" instead of a proper "Ozolu iela 9"
        
        if (EndsWithStreetNameSuffix(tagStreet, out string? tagSuffix))
            tagStreet = tagStreet.Replace(tagSuffix!, "").Trim();
        
        if (!ContainsStreetNameSuffix(fuzzyAddress, out string? fuzzySuffix))
        {
            // We are something like "Ozolu 9"
            if (!fuzzyAddress.Contains(tagStreet))
                return false;
        }
        else
        {
            // We are something like "Ozolu gatve 9" but the tag is something like "Ozolu iela"
            if (fuzzySuffix != tagSuffix)
                return false;
        }

        // TODO: "Kr.Barona"
        // TODO: "A. Deglava iela"
        
        // Number
        
        if (tagHouseNumber == null)
            return false;
        
        tagHouseNumber = tagHouseNumber.ToLower();

        // Don't know where the number is, but let's hope if somewhere 
        
        MatchCollection matches = Regex.Matches(fuzzyAddress, @"\d+[a-z]?", RegexOptions.IgnoreCase);
        // 13
        // 13B

        if (matches.Count == 0)
            return false;

        if (matches.All(m => m.ToString().ToLower() != tagHouseNumber))
            return false;

        // If OSM has unit, and fuzzy address contains explicit unit (e.g., 5-3), they must match
        if (tagUnit != null)
        {
            Match unitMatch = Regex.Match(fuzzyAddress, @"\b\d+[a-z]?\s*-\s*(?<unit>\d+)\b", RegexOptions.IgnoreCase);
            if (unitMatch.Success)
            {
                string unit = unitMatch.Groups["unit"].Value.Trim();
                if (!string.Equals(unit, tagUnit, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        return true;
    }

    [Pure]
    public static bool EndsWithStreetNameSuffix(string streetName)
    {
        return EndsWithStreetNameSuffix(streetName, out string _);
    }
    
    [Pure]
    public static bool EndsWithStreetNameSuffix(string streetName, out string? suffix)
    {
        foreach (string s in _suffixes)
        {
            if (streetName.EndsWith(" " + s))
            {
                suffix = s;
                return true;
            }
        }

        suffix = null;
        return false;
    }

    [Pure]
    public static bool ContainsStreetNameSuffix(string address, out string? suffix)
    {
        foreach (string s in _suffixes)
        {
            if (address.Contains(s))
            {
                suffix = s;
                return true;
            }
        }

        suffix = null;
        return false;
    }
}