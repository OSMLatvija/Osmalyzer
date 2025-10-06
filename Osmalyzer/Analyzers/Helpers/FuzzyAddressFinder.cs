namespace Osmalyzer;

public static class FuzzyAddressFinder
{
    // TODO: TESTS, this is getting complex
    
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

        // Try preselect candidates by a single high-confidence regional hint (e.g. parish)
        // Will fall back to full search if no such hint exists or no candidates found
        List<IEnumerable<Addressable>> candidateGroups = GetFilteredCandidates(parsed);
        // todo: retry with all elements if poor score

        foreach (IEnumerable<Addressable> candidates in candidateGroups)
        {
            // Match against OSM elements

            List<OsmElement> matchedElements = [ ];
            int? bestScore = null;

            foreach (Addressable addressable in candidates)
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
                    FuzzyAddressStreetNameAndNumberPart? streetMatch = GetBestMatch(addressable.Street, parsed.StreetNameAndNumberParts, p => p.StreetValue);
                    FuzzyAddressStreetNameAndNumberPart? numberMatch = GetBestMatch(addressable.Number, parsed.StreetNameAndNumberParts, p => p.NumberValue);
                    FuzzyAddressStreetNameAndNumberPart? unitMatch = GetBestMatch(addressable.Unit, parsed.StreetNameAndNumberParts, p => p.UnitValue);
                    FuzzyAddressCityPart? cityMatch = GetBestMatch(addressable.City, parsed.CityParts, p => p.Value);
                    FuzzyAddressParishPart? parishMatch = GetBestMatch(addressable.Parish, parsed.ParishParts, p => p.Value);
                    FuzzyAddressMunicipalityPart? municipalityMatch = GetBestMatch(addressable.Municipality, parsed.MunicipalityParts, p => p.Value);
                    FuzzyAddressPostcodePart? postcodeMatch = GetBestMatch(addressable.Postcode, parsed.PostcodeParts, p => p.Value);

                    // Try old values if current are not fully matched

                    bool old = false;
                    if (streetMatch == null || numberMatch == null || houseNameMatch == null) // unit is rarely present
                    {
                        FuzzyAddressHouseNamePart? oldHouseNameMatch = GetBestMatch(addressable.OldHouseName, parsed.HouseNameParts, p => p.Value);
                        FuzzyAddressStreetNameAndNumberPart? oldStreetMatch = GetBestMatch(addressable.OldStreet, parsed.StreetNameAndNumberParts, p => p.StreetValue);
                        FuzzyAddressStreetNameAndNumberPart? oldNumberMatch = GetBestMatch(addressable.OldNumber, parsed.StreetNameAndNumberParts, p => p.NumberValue);
                        FuzzyAddressStreetNameAndNumberPart? oldUnitMatch = GetBestMatch(addressable.OldUnit, parsed.StreetNameAndNumberParts, p => p.UnitValue);

                        int oldMatches = 0;
                        if (oldHouseNameMatch != null)
                        {
                            houseNameMatch = oldHouseNameMatch;
                            oldMatches++;
                        }

                        if (oldStreetMatch != null)
                        {
                            streetMatch = oldStreetMatch;
                            oldMatches++;
                        }

                        if (oldNumberMatch != null)
                        {
                            numberMatch = oldNumberMatch;
                            oldMatches++;
                        }

                        if (oldUnitMatch != null)
                        {
                            unitMatch = oldUnitMatch;
                            oldMatches++;
                        }

                        int currentMatches = 0;
                        if (houseNameMatch != null) currentMatches++;
                        if (streetMatch != null) currentMatches++;
                        if (numberMatch != null) currentMatches++;
                        if (unitMatch != null) currentMatches++;

                        if (oldMatches >= currentMatches)
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

            if (matchedElements.Count != 0)
                return OsmGeoTools.GetAverageCoord(matchedElements);
        }
        
        return null; // no candidates found at all
    }

    
    private static List<IEnumerable<Addressable>> GetFilteredCandidates(FuzzyAddress address)
    {
        List<IEnumerable<Addressable>> candidates = [ ];
        
        // Priority: Parish -> City -> Municipality. Only when exactly one part exists and it's high-confidence.

        string? parish = address.SingleParishPart?.Confidence >= FuzzyConfidence.High ? address.SingleParishPart.Value : null;
        if (parish != null)
        {
            IEnumerable<Addressable>? list = _addressables!.GetByParish(parish);
            if (list != null) candidates.Add(list);
        }

        string? city = address.SingleCityPart?.Confidence >= FuzzyConfidence.High ? address.SingleCityPart.Value : null;
        if (city != null)
        {
            IEnumerable<Addressable>? list = _addressables!.GetByCity(city);
            if (list != null) candidates.Add(list);
        }

        string? municipality = address.SingleMunicipalityPart?.Confidence >= FuzzyConfidence.High ? address.SingleMunicipalityPart.Value : null;
        if (municipality != null)
        {
            IEnumerable<Addressable>? list = _addressables!.GetByMunicipality(municipality);
            if (list != null) candidates.Add(list);
        }

        // As the last resort, no confident single-region candidate; use full search
        candidates.Add(_addressables!.Elements);
        return candidates;
    }

    private static void GatherAddressables(OsmMasterData data)
    {
        OsmDataExtract extract = data.Filter(new HasKey("ref:LV:addr"));
        _addressables = new Addressables(extract.Elements);
    }


    private sealed class Addressables
    {
        public List<Addressable> Elements { get; }

        
        private readonly Dictionary<string, List<Addressable>> _byParish = new Dictionary<string, List<Addressable>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<Addressable>> _byCity = new Dictionary<string, List<Addressable>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<Addressable>> _byMunicipality = new Dictionary<string, List<Addressable>>(StringComparer.OrdinalIgnoreCase);
        
        
        public Addressables(IEnumerable<OsmElement> elements)
        {
            Elements = elements.Select(e => new Addressable(e)).ToList();

            // Build caches
            
            foreach (Addressable addressable in Elements)
            {
                if (addressable.Parish != null)
                {
                    if (!_byParish.TryGetValue(addressable.Parish, out List<Addressable>? list))
                    {
                        list = [ ];
                        _byParish[addressable.Parish] = list;
                    }
                    list.Add(addressable);
                }

                if (addressable.City != null)
                {
                    if (!_byCity.TryGetValue(addressable.City, out List<Addressable>? list))
                    {
                        list = [ ];
                        _byCity[addressable.City] = list;
                    }
                    list.Add(addressable);
                }

                if (addressable.Municipality != null)
                {
                    if (!_byMunicipality.TryGetValue(addressable.Municipality, out List<Addressable>? list))
                    {
                        list = [ ];
                        _byMunicipality[addressable.Municipality] = list;
                    }
                    list.Add(addressable);
                }
            }
        }

        
        public IEnumerable<Addressable>? GetByParish(string value) => 
            _byParish.TryGetValue(value, out List<Addressable>? list) ? list : null;

        public IEnumerable<Addressable>? GetByCity(string value) => 
            _byCity.TryGetValue(value, out List<Addressable>? list) ? list : null;

        public IEnumerable<Addressable>? GetByMunicipality(string value) => 
            _byMunicipality.TryGetValue(value, out List<Addressable>? list) ? list : null;
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