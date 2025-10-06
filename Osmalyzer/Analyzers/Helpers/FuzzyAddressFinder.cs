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
        
        FuzzyAddressHouseNamePart[] houseNameParts = parts.OfType<FuzzyAddressHouseNamePart>().ToArray();
        FuzzyAddressStreetNameAndNumberPart[] nameAndNumberParts = parts.OfType<FuzzyAddressStreetNameAndNumberPart>().ToArray();
        FuzzyAddressCityPart[] cityParts = parts.OfType<FuzzyAddressCityPart>().ToArray();
        FuzzyAddressParishPart[] parishParts = parts.OfType<FuzzyAddressParishPart>().ToArray();
        FuzzyAddressMunicipalityPart[] municipalityParts = parts.OfType<FuzzyAddressMunicipalityPart>().ToArray();
        FuzzyAddressPostcodePart[] postcodeParts = parts.OfType<FuzzyAddressPostcodePart>().ToArray();
        
        // Match against OSM elements
        
        List<OsmElement> matchedElements = [ ];
        int? bestScore = null; 
        
        foreach (OsmElement element in _addressables!)
        {
            if (DoesElementMatch(out int score))
            {
                if (bestScore == null || score > bestScore)
                {
                    matchedElements.Clear(); // new best match list
                    matchedElements.Add(element);
                    bestScore = score;
                }
                else if (score == bestScore)
                {
                    matchedElements.Add(element);
                }
            }

            continue;


            [Pure]
            bool DoesElementMatch(out int score)
            {
                string? elementHouseName = element.GetValue("addr:housename");
                string? elementStreet = element.GetValue("addr:street");
                string? elementNumber = element.GetValue("addr:housenumber");
                string? elementCity = element.GetValue("addr:city");
                string? elementParish = element.GetValue("addr:subdistrict");
                string? elementMunicipality = element.GetValue("addr:district");
                string? elementPostcode = element.GetValue("addr:postcode");

                FuzzyAddressHouseNamePart? houseNameMatch = GetBestMatch(elementHouseName, houseNameParts, p => p.Value);
                FuzzyAddressStreetNameAndNumberPart? streetMatch = GetBestMatch(elementStreet, nameAndNumberParts, p => p.StreetValue);
                FuzzyAddressStreetNameAndNumberPart? numberMatch = GetBestMatch(elementNumber, nameAndNumberParts, p => p.NumberValue);
                FuzzyAddressCityPart? cityMatch = GetBestMatch(elementCity, cityParts, p => p.Value);
                FuzzyAddressParishPart? parishMatch = GetBestMatch(elementParish, parishParts, p => p.Value);
                FuzzyAddressMunicipalityPart? municipalityMatch = GetBestMatch(elementMunicipality, municipalityParts, p => p.Value);
                FuzzyAddressPostcodePart? postcodeMatch = GetBestMatch(elementPostcode, postcodeParts, p => p.Value);

                static T? GetBestMatch<T>(string? elementValue, T[] source, Func<T, string> valueSelector) where T : FuzzyAddressPart
                {
                    if (elementValue == null)
                        return null;

                    return source
                           .Where(p =>
                                      valueSelector(p).Equals(elementValue, StringComparison.OrdinalIgnoreCase) // todo: other logic?
                           )
                           .OrderByDescending(p => p.Confidence)
                           .FirstOrDefault();
                }
                
                bool houseNameMatched = houseNameMatch != null;
                bool streetMatched = streetMatch != null;
                bool numberMatched = numberMatch != null;
                bool cityMatched = cityMatch != null;
                bool parishMatched = parishMatch != null;
                bool municipalityMatched = municipalityMatch != null;
                bool postcodeMatched = postcodeMatch != null;

                // Calculate approximate match "quality"
                // This is all very hand-wavy and based on what sort of broken syntax addresses are present in data
                score = 0;
                if (houseNameMatched) score += 10;
                if (streetMatched) score += 10;
                if (numberMatched) score += 10;
                if (cityMatched) score += 5;
                if (parishMatched) score += 5;
                if (municipalityMatched) score += 5;
                if (postcodeMatched) score += 5;
                
                // todo: I tried discarding high-confidence parts against non-matching element values,
                // but this just skips so many addresses, it's not worth it with data being so messy 
                
                // Minimum matching requirements
                // Street lines can repeat between cities/towns, but (presumably) not withing the same area
                // So "Vidus iela 1" is not enough because half the places in Latvia have a "Vidus iela"
                // todo: what are the actual address restriction in Latvia for this?
                bool streetLineMatched = houseNameMatched || streetMatched && numberMatched;
                return streetLineMatched && (cityMatched || parishMatched || postcodeMatched);
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