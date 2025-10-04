namespace Osmalyzer;

public static class FuzzyAddressFinder
{
    private static List<OsmElement>? _addressables;


    /// <summary>
    /// 
    /// </summary>
    public static OsmCoord? Find(OsmMasterData data, string address, params FuzzyAddressHint[] hints)
    {
        List<FuzzyAddressPart>? parts = FuzzyAddressParser.TryParseAddress(address, hints);

        if (parts == null)
            return null; // could not parse address at all
        
        if (_addressables == null)
            GatherAddressables(data);
        
        // Get the rawish address part values
        
        string? houseName = parts.OfType<FuzzyAddressHouseNamePart>().FirstOrDefault()?.Value;
        FuzzyAddressStreetNameAndNumberPart? nameAndNumberPart = parts.OfType<FuzzyAddressStreetNameAndNumberPart>().FirstOrDefault();
        string? streetName = nameAndNumberPart?.StreetValue;
        string? streetNumber = nameAndNumberPart?.NumberValue;
        string? city = parts.OfType<FuzzyAddressCityPart>().FirstOrDefault()?.Value;
        string? postcode = parts.OfType<FuzzyAddressPostcodePart>().FirstOrDefault()?.Value;
        
        // Match against OSM elements
        
        List<OsmElement> matchedElements = [ ];
        
        foreach (OsmElement element in _addressables!)
        {
            if (DoesElementMatch())
                matchedElements.Add(element);

            continue;


            [Pure]
            bool DoesElementMatch()
            {
                bool houseNameMatched = houseName != null && houseName == element.GetValue("addr:housename");
                bool streetMatched = streetName != null && streetName == element.GetValue("addr:street");
                bool numberMatched = streetNumber != null && streetNumber == element.GetValue("addr:housenumber");
                bool cityMatched = city != null && city == element.GetValue("addr:city");
                bool postcodeMatched = postcode != null && postcode == element.GetValue("addr:postcode");
                
                bool streetLineMatched = houseNameMatched || streetMatched && numberMatched;
                
                // Street line can repeat between cities/towns, but (presumably) not withing the same city/town or postcode
                
                return streetLineMatched && (cityMatched || postcodeMatched);
            }
        }
        
        if (matchedElements.Count == 0)
            return null; // no matches found

        return OsmGeoTools.GetAverageCoord(matchedElements);
    }

    // todo: convert to above
    /// <summary>
    /// Find an address point in OSM data based on matching of address.
    /// </summary>
    public static OsmCoord? Find(OsmMasterData data, string? name, string? location, string? pagasts, string? novads, string postalCode)
    {
        if (_addressables == null)
            GatherAddressables(data);

        string? houseName = null;

        // Name could explicitly be a house name
        if (name != null)
        {
            // ""Palmas"" 
            if (name.StartsWith('\"') && name.EndsWith('\"'))
            {
                houseName = name[1..^1];
                name = null; // name is nothing else then
            }
        }
        
        string? streetName = null;
        string? streetNumber = null;

        // Name could be a street number line 
        if (name != null)
        {
            // "Andreja Upīša iela 1"
            // "Skolas iela 1A"
            // "Gaujas iela 33a"
            // "Raiņa iela 23 A"
            
            Match suffixNumberMatch = Regex.Match(name, @"\d+(?: ?[a-zA-Z])?$");

            if (suffixNumberMatch.Success)
            {
                streetName = name[..^suffixNumberMatch.Length].Trim();
                streetNumber = suffixNumberMatch.Value.Replace(" ", "").Trim();
            }
        }

        List<OsmElement> matchedElements = [ ];
        int bestMatchedScore = 0;
        
        foreach (OsmElement element in _addressables!)
        {
            if (DoesElementMatch(out int matchScore))
            {
                if (matchedElements.Count == 0)
                {
                    matchedElements.Add(element);
                    bestMatchedScore = matchScore;
                }
                else if (matchScore > bestMatchedScore)
                {
                    matchedElements.Clear();
                    matchedElements.Add(element);
                    bestMatchedScore = matchScore;
                }
                else if (matchScore == bestMatchedScore)
                {
                    matchedElements.Add(element);
                }
            }

            continue;


            [Pure]
            bool DoesElementMatch(out int score)
            {
                score = 0;
                
                // Street "line" - house name or street + number
                
                if (name != null && element.GetValue("addr:housename")?.ToLower() == name.ToLower())
                    score += 10;
                    
                else if (houseName != null && element.GetValue("addr:housename")?.ToLower() == houseName.ToLower())
                    score += 10;

                else if (streetName != null && streetNumber != null &&
                         element.GetValue("addr:street")?.ToLower() == streetName.ToLower() &&
                         element.GetValue("addr:housenumber")?.ToLower() == streetNumber.ToLower())
                    score += 10;
                
                // Location
                
                if (location != null && element.GetValue("addr:city") == location)
                    score += 15;
                else if (pagasts != null && element.GetValue("addr:subdistrict") == pagasts)
                    score += 12;
                else if (element.GetValue("addr:postcode") == postalCode) // fallback, basically
                    score += 10;
                
                // nvoads/addr:district is a bit too broad and we expect OSM addresses to have pagasts/subdistrict or city
                
                return score >= 20; // good enough
            }
        }
        
        if (matchedElements.Count == 0)
            return null; // no matches found

        return OsmGeoTools.GetAverageCoord(matchedElements);
    }


    private static void GatherAddressables(OsmMasterData data)
    {
        OsmDataExtract extract = data.Filter(new HasKey("ref:LV:addr"));
        _addressables = extract.Elements.ToList();
    }
}