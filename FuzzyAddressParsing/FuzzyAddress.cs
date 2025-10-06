namespace Osmalyzer;

/// <summary>
/// Parsed fuzzy address from <see cref="FuzzyAddressParser"/>.
/// </summary>
public record FuzzyAddress
{
    /// <summary>
    /// All parsed parts in their original order.
    /// </summary>
    public List<FuzzyAddressPart> Parts { get; }

    /// <summary>
    /// Cached list of house name parts, or null if none present.
    /// Presorted by <see cref="FuzzyAddressPart.Confidence"/> descending.
    /// </summary>
    public FuzzyAddressHouseNamePart[]? HouseNameParts { get; }

    /// <summary>
    /// Cached list of street name and number parts, or null if none present.
    /// Presorted by <see cref="FuzzyAddressPart.Confidence"/> descending.
    /// </summary>
    public FuzzyAddressStreetNameAndNumberPart[]? StreetNameAndNumberParts { get; }

    /// <summary>
    /// Cached list of city parts, or null if none present.
    /// Presorted by <see cref="FuzzyAddressPart.Confidence"/> descending.
    /// </summary>
    public FuzzyAddressCityPart[]? CityParts { get; }

    /// <summary>
    /// Cached list of parish parts, or null if none present.
    /// Presorted by <see cref="FuzzyAddressPart.Confidence"/> descending.
    /// </summary>
    public FuzzyAddressParishPart[]? ParishParts { get; }

    /// <summary>
    /// Cached list of municipality parts, or null if none present.
    /// Presorted by <see cref="FuzzyAddressPart.Confidence"/> descending.
    /// </summary>
    public FuzzyAddressMunicipalityPart[]? MunicipalityParts { get; }

    /// <summary>
    /// Cached list of postcode parts, or null if none present.
    /// Presorted by <see cref="FuzzyAddressPart.Confidence"/> descending.
    /// </summary>
    public FuzzyAddressPostcodePart[]? PostcodeParts { get; }

    /// <summary>
    /// Cached single parish part when exactly one is present, otherwise null.
    /// </summary>
    public FuzzyAddressParishPart? SingleParishPart { get; }

    /// <summary>
    /// Cached single city part when exactly one is present, otherwise null.
    /// </summary>
    public FuzzyAddressCityPart? SingleCityPart { get; }

    /// <summary>
    /// Cached single municipality part when exactly one is present, otherwise null.
    /// </summary>
    public FuzzyAddressMunicipalityPart? SingleMunicipalityPart { get; }


    private static readonly List<FuzzyAddressHouseNamePart> _tmpHouseNames = [ ];
    private static readonly List<FuzzyAddressStreetNameAndNumberPart> _tmpStreetNameAndNumbers = [ ];
    private static readonly List<FuzzyAddressCityPart> _tmpCities = [ ];
    private static readonly List<FuzzyAddressParishPart> _tmpParishes = [ ];
    private static readonly List<FuzzyAddressMunicipalityPart> _tmpMunicipalities = [ ];
    private static readonly List<FuzzyAddressPostcodePart> _tmpPostcodes = [ ];
    
    
    public FuzzyAddress(List<FuzzyAddressPart> parts)
    {
        if (parts == null) throw new ArgumentNullException(nameof(parts));
        if (parts.Count == 0) throw new ArgumentException("Must have at least one part.", nameof(parts));
        
        
        Parts = parts;

        foreach (FuzzyAddressPart part in parts)
        {
            switch (part)
            {
                case FuzzyAddressHouseNamePart hn: _tmpHouseNames.Add(hn); break;
                case FuzzyAddressStreetNameAndNumberPart snn: _tmpStreetNameAndNumbers.Add(snn); break;
                case FuzzyAddressCityPart c: _tmpCities.Add(c); break;
                case FuzzyAddressParishPart p: _tmpParishes.Add(p); break;
                case FuzzyAddressMunicipalityPart m: _tmpMunicipalities.Add(m); break;
                case FuzzyAddressPostcodePart pc: _tmpPostcodes.Add(pc); break;
            }
        }

        // Create arrays only for found entries; keep null to indicate "not found"
        if (_tmpHouseNames.Count == 0) HouseNameParts = null; else { HouseNameParts = _tmpHouseNames.ToArray(); _tmpHouseNames.Clear(); SortByConfidenceDesc(HouseNameParts); }
        if (_tmpStreetNameAndNumbers.Count == 0) StreetNameAndNumberParts = null; else { StreetNameAndNumberParts = _tmpStreetNameAndNumbers.ToArray(); _tmpStreetNameAndNumbers.Clear(); SortByConfidenceDesc(StreetNameAndNumberParts); }
        if (_tmpCities.Count == 0) CityParts = null; else { CityParts = _tmpCities.ToArray(); _tmpCities.Clear(); SortByConfidenceDesc(CityParts); }
        if (_tmpParishes.Count == 0) ParishParts = null; else { ParishParts = _tmpParishes.ToArray(); _tmpParishes.Clear(); SortByConfidenceDesc(ParishParts); }
        if (_tmpMunicipalities.Count == 0) MunicipalityParts = null; else { MunicipalityParts = _tmpMunicipalities.ToArray(); _tmpMunicipalities.Clear(); SortByConfidenceDesc(MunicipalityParts); }
        if (_tmpPostcodes.Count == 0) PostcodeParts = null; else { PostcodeParts = _tmpPostcodes.ToArray(); _tmpPostcodes.Clear(); SortByConfidenceDesc(PostcodeParts); }

        // Cache single region parts when exactly one exists
        SingleParishPart = ParishParts != null && ParishParts.Length == 1 ? ParishParts[0] : null;
        SingleCityPart = CityParts != null && CityParts.Length == 1 ? CityParts[0] : null;
        SingleMunicipalityPart = MunicipalityParts != null && MunicipalityParts.Length == 1 ? MunicipalityParts[0] : null;
    }

    
    private static void SortByConfidenceDesc<T>(T[] parts) where T : FuzzyAddressPart => 
        Array.Sort(parts, (a, b) => b.Confidence.CompareTo(a.Confidence));
}
