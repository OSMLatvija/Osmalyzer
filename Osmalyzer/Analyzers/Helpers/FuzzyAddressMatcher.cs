using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer;

public static class FuzzyAddressMatcher
{
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
        "pārvads"
    };
    
    
    [Pure]
    public static bool Matches(OsmElement element, string fuzzyAddress)
    {
        return Matches(
            element.GetValue("addr:street"),
            element.GetValue("addr:housenumber"),
            fuzzyAddress
        );
    }
    
    [Pure]
    public static bool Matches(string? tagStreet, string? tagHouseNumber, string fuzzyAddress)
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
            if (streetName.EndsWith(s))
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