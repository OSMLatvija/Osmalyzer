namespace Osmalyzer;

public static class FuzzyAddressFinder
{
    private static Addressables? _addressables;


    /// <summary>
    /// 
    /// </summary>
    /// <param name="data">It is presumed that full OSM (address) data is given once per session</param>
    public static OsmCoord? Find(OsmMasterData data, string address, params FuzzyAddressHint[] hints)
    {
        FuzzyAddress? parsed = FuzzyAddressParser.TryParseAddress(address, hints);

        if (parsed == null)
            return null; // could not parse address at all
        
        if (_addressables == null)
            GatherAddressables(data);
        
        // Match against OSM elements
        
        List<OsmElement> matchedElements = [ ];
        int? bestScore = null; 
        
        foreach (Addressable addressable in _addressables!.Elements)
        {
            if (DoesElementMatch(out int elementScore))
            {
                if (bestScore == null || elementScore > bestScore)
                {
                    matchedElements.Clear(); // new best match list
                    matchedElements.Add(addressable.Element);
                    bestScore = elementScore;
                }
                else if (elementScore == bestScore)
                {
                    matchedElements.Add(addressable.Element);
                }
            }

            continue;


            [Pure]
            bool DoesElementMatch(out int score)
            {
                // Gather all the matches between cached OSM values and parsed parts using cached arrays (null -> not found)
                
                FuzzyAddressHouseNamePart? houseNameMatch = GetBestMatch(addressable.HouseName, parsed.HouseNameParts, p => p.Value);
                if (houseNameMatch == null && addressable.OldHouseName != null)
                    houseNameMatch = GetBestMatch(addressable.OldHouseName, parsed.HouseNameParts, p => p.Value);
                FuzzyAddressStreetNameAndNumberPart? streetMatch = GetBestMatch(addressable.Street, parsed.StreetNameAndNumberParts, p => p.StreetValue);
                FuzzyAddressStreetNameAndNumberPart? numberMatch = GetBestMatch(addressable.Number, parsed.StreetNameAndNumberParts, p => p.NumberValue);
                FuzzyAddressStreetNameAndNumberPart? unitMatch = GetBestMatch(addressable.Unit, parsed.StreetNameAndNumberParts, p => p.UnitValue);
                FuzzyAddressCityPart? cityMatch = GetBestMatch(addressable.City, parsed.CityParts, p => p.Value);
                FuzzyAddressParishPart? parishMatch = GetBestMatch(addressable.Parish, parsed.ParishParts, p => p.Value);
                FuzzyAddressMunicipalityPart? municipalityMatch = GetBestMatch(addressable.Municipality, parsed.MunicipalityParts, p => p.Value);
                FuzzyAddressPostcodePart? postcodeMatch = GetBestMatch(addressable.Postcode, parsed.PostcodeParts, p => p.Value);
                
                // Try old values if current are different/unmatched
                
                bool old = false;
                if (streetMatch == null && numberMatch == null && houseNameMatch == null) // implies unit also not matched
                {
                    // If nothing matched, try (all on) old_addr:*
                    houseNameMatch = GetBestMatch(addressable.OldHouseName, parsed.HouseNameParts, p => p.Value);
                    streetMatch = GetBestMatch(addressable.OldStreet, parsed.StreetNameAndNumberParts, p => p.StreetValue);
                    numberMatch = GetBestMatch(addressable.OldNumber, parsed.StreetNameAndNumberParts, p => p.NumberValue);
                    unitMatch = GetBestMatch(addressable.OldUnit, parsed.StreetNameAndNumberParts, p => p.UnitValue);
                    old = true;
                }

                static T? GetBestMatch<T>(string? elementValue, T[]? source, Func<T, string?> valueSelector) where T : FuzzyAddressPart
                {
                    if (elementValue == null)
                        return null;

                    if (source == null) // can't be 0 if set
                        return null;

                    return source.FirstOrDefault(p => valueSelector(p)?.Equals(elementValue, StringComparison.OrdinalIgnoreCase) == true);
                }
                
                // Decide on what matched
                
                bool houseNameMatched = houseNameMatch != null;
                bool streetMatched = streetMatch != null;
                bool numberMatched = numberMatch != null;
                bool cityMatched = cityMatch != null;
                bool parishMatched = parishMatch != null;
                bool municipalityMatched = municipalityMatch != null;
                bool postcodeMatched = postcodeMatch != null;
                bool unitMatched = unitMatch != null;
                
                // todo: I tried discarding high-confidence parts against non-matching element values,
                // but this just skips so many addresses, it's not worth it with data being so messy 
                
                score = 0;
                
                // Try to pass minimum matching requirements
                // Street lines can repeat between cities/towns, but (presumably) not withing the same area
                // So "Vidus iela 1" is not enough because half the places in Latvia have a "Vidus iela"
                // todo: what are the actual address restriction in Latvia for this?
                
                bool streetLineMatched = houseNameMatched || streetMatched && numberMatched;
                if (!streetLineMatched || (!cityMatched && !parishMatched && !postcodeMatched))
                    return false;
                
                // Calculate approximate match "quality"
                // This is all very hand-wavy and based on what sort of broken syntax addresses are present in data
                
                if (houseNameMatched) score += old ? 10 : 20;
                if (streetMatched) score += old ? 5 : 10;
                if (numberMatched) score += old ? 5 : 10;
                if (unitMatched) score += 2; // I guess if there are units on OSM, this is better (assuming everything else is the same)
                if (cityMatched) score += 5;
                if (parishMatched) score += 5;
                if (municipalityMatched) score += 5;
                if (postcodeMatched) score += 5;
                
                return true;
            }
        }
        
        if (matchedElements.Count == 0)
            return null; // no matches found

        return OsmGeoTools.GetAverageCoord(matchedElements);
    }

    // todo: convert to above


    private static void GatherAddressables(OsmMasterData data)
    {
        OsmDataExtract extract = data.Filter(new HasKey("ref:LV:addr"));
        _addressables = new Addressables(extract.Elements);
    }


    private sealed class Addressables
    {
        public List<Addressable> Elements { get; }

        
        public Addressables(IEnumerable<OsmElement> elements)
        {
            Elements = elements.Select(e => new Addressable(e)).ToList();
        }
    }

    private sealed class Addressable
    {
        public OsmElement Element { get; }

        public string? HouseName { get; }
        public string? Street { get; }
        public string? Number { get; }
        public string? Unit { get; }
        public string? City { get; }
        public string? Parish { get; }
        public string? Municipality { get; }
        public string? Postcode { get; }
        public string? OldStreet { get; }
        public string? OldNumber { get; }
        public string? OldHouseName { get; }
        public string? OldUnit { get; }


        public Addressable(OsmElement element)
        {
            Element = element;

            HouseName = element.GetValue("addr:housename");
            Street = element.GetValue("addr:street");
            Number = element.GetValue("addr:housenumber");
            Unit = element.GetValue("addr:unit");
            City = element.GetValue("addr:city");
            Parish = element.GetValue("addr:subdistrict");
            Municipality = element.GetValue("addr:district");
            Postcode = element.GetValue("addr:postcode");
            OldStreet = element.GetValue("old_addr:street");
            OldNumber = element.GetValue("old_addr:housenumber");
            OldHouseName = element.GetValue("old_addr:housename");
            OldUnit = element.GetValue("old_addr:unit");
        }
    }
}