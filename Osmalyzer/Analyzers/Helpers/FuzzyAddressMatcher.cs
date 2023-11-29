using System.Linq;
using System.Text.RegularExpressions;

namespace Osmalyzer;

public static class FuzzyAddressMatcher
{
    public static bool Matches(OsmElement element, string address)
    {
        address = address.Trim().ToLower();
        
        if (address == "")
            return false;
        
        // Street
        
        string? street = element.GetValue("addr:street");
        
        if (street == null)
            return false;

        street = street.ToLower();

        // Addresses often do "Sarkandaugavas 9" instead of a proper "Sarkandaugavas iela 9"
        
        if (EndsWithStreetNameSuffix(street, out string? suffix))
            street = street.Replace(suffix!, "").Trim();
        
        if (!address.Contains(street))
            return false;
        
        // TODO: "Kr.Barona"
        
        // Number
        
        string? number = element.GetValue("addr:housenumber");

        if (number == null)
            return false;

        // Don't know where the number is, but let's hope if somewhere 
        
        MatchCollection matches = Regex.Matches(address, @"\d+[a-z]?");
        // 13
        // 13B

        if (matches.Count == 0)
            return false;

        if (matches.All(m => m.ToString().ToLower() != number))
            return false;

        return true;
    }

    public static bool EndsWithStreetNameSuffix(string streetName)
    {
        return EndsWithStreetNameSuffix(streetName, out string _);
    }
    
    public static bool EndsWithStreetNameSuffix(string streetName, out string? suffix)
    {
        string[] suffixes = 
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

        foreach (string s in suffixes)
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
}