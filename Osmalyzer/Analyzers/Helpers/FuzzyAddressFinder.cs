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
        
        foreach (OsmElement element in _addressables!)
        {
            if (DoesElementMatch())
                matchedElements.Add(element);

            continue;


            [Pure]
            bool DoesElementMatch()
            {
                string? elementHouseName = element.GetValue("addr:housename");
                string? elementStreet = element.GetValue("addr:street");
                string? elementNumber = element.GetValue("addr:housenumber");
                string? elementCity = element.GetValue("addr:city");
                string? elementParish = element.GetValue("addr:subdistrict");
                string? elementMunicipality = element.GetValue("addr:district");
                string? elementPostcode = element.GetValue("addr:postcode");
                
                bool houseNameMatched = elementHouseName != null && houseNameParts.Any(p => p.Value.Equals(elementHouseName, StringComparison.OrdinalIgnoreCase));
                bool streetMatched = elementStreet != null && nameAndNumberParts.Any(p => p.StreetValue.Equals(elementStreet, StringComparison.OrdinalIgnoreCase));
                bool numberMatched = elementNumber != null && nameAndNumberParts.Any(p => p.NumberValue.Equals(elementNumber, StringComparison.OrdinalIgnoreCase));
                bool cityMatched = elementCity != null && cityParts.Any(p => p.Value.Equals(elementCity, StringComparison.OrdinalIgnoreCase));
                bool parishMatched = elementParish != null && parishParts.Any(p => p.Value.Equals(elementParish, StringComparison.OrdinalIgnoreCase));
                bool municipalityMatched = elementMunicipality != null && municipalityParts.Any(p => p.Value.Equals(elementMunicipality, StringComparison.OrdinalIgnoreCase));
                bool postcodeMatched = elementPostcode != null && postcodeParts.Any(p => p.Value.Equals(elementPostcode, StringComparison.OrdinalIgnoreCase));

                // If values are present and also parts are strongly-matched, then require them to match, otherwise it'd definitely wrong
                if (ConflictingValuesFor(cityParts, p => p.Value, elementCity)) return false;
                if (ConflictingValuesFor(parishParts, p => p.Value, elementParish)) return false;
                if (ConflictingValuesFor(municipalityParts, p => p.Value, elementMunicipality)) return false;
                if (ConflictingValuesFor(postcodeParts, p => p.Value, elementPostcode)) return false;
                
                static bool ConflictingValuesFor<T>(T[] parts, Func<T, string> elementValueGetter, string? value) where T : FuzzyAddressPart
                {
                    if (value == null)
                        return false; // no conflict, value not present
                    
                    if (parts.Length == 0)
                        return false; // no conflict, no parts to compare against

                    T[] strongParts = parts.Where(p => p.Confidence is FuzzyConfidence.High or FuzzyConfidence.HintedHigh).ToArray();

                    if (strongParts.Length == 0)
                        return false; // no conflict, no strong parts to compare against

                    foreach (T p in strongParts)
                        if (elementValueGetter(p).Equals(value, StringComparison.OrdinalIgnoreCase)) // todo: custom matching for values?
                            return false; // exact match found, no conflict
                    
                    return true; // conflict, strong parts exist but none match
                }

                bool streetLineMatched = houseNameMatched || streetMatched && numberMatched;
                
                // Street lines can repeat between cities/towns, but (presumably) not withing the same area
                // So "Vidus iela 1" is not enough because half the places in Latvia have a "Vidus iela"
                // todo: what are the actual address restriction in Latvia for this?

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