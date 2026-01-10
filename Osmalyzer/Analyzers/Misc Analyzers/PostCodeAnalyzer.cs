namespace Osmalyzer;

[UsedImplicitly]
public class PostCodeAnalyzer : Analyzer
{
    public override string Name => "Post Codes";

    public override string Description => "This report analyzes post code use. Post codes come from address information (which is from offical VZD data). " +
                                          "Note that this assumes the post office services the same region as its own address post code. " +
                                          "This also assumes addresses were assigned correctly to the nodes (e.g. post offices are at the right location).";

    public override AnalyzerGroup Group => AnalyzerGroup.Miscellaneous;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];

    
    private static OsmPolygon _latviaPolygon = null!;


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData OsmData = osmData.MasterData;

        OsmData postcodedElements = OsmData.Filter(
            new HasKey("addr:postcode")
        );
        // Will filter by boundary only when checking invalid ones, otherwise it's way too slow for every address
        
        _latviaPolygon = BoundaryHelper.GetLatviaPolygon(osmData.MasterData);

        OsmData postOffices = OsmData.Filter(
            new HasValue("amenity", "post_office"),
            new InsidePolygon(_latviaPolygon, OsmPolygon.RelationInclusionCheck.FuzzyLoose)
        );
        
        postcodedElements = postcodedElements.Subtract(postOffices); // don't include post offices in the regular post code analysis

        const double distantElementThreshold = 50_000; // meters

        // Prepare groups

        report.AddGroup(ReportGroup.Regions, "Post code regions");

        report.AddEntry(
            ReportGroup.Regions,
            new DescriptionReportEntry(
                "Post code regions (average coord from all found addresses)."
            )
        );

        report.AddGroup(ReportGroup.InvalidCodes, "Invalid post codes");

        report.AddEntry(
            ReportGroup.InvalidCodes,
            new DescriptionReportEntry(
                "These do not match expected syntax for Latvia - `LV-####`."
            )
        );
        
        report.AddEntry(
            ReportGroup.InvalidCodes,
            new PlaceholderReportEntry(
                "All post codes appear to be valid."
            )
        );

        report.AddGroup(ReportGroup.PostOffices, "Post offices");

        report.AddEntry(
            ReportGroup.PostOffices,
            new DescriptionReportEntry(
                "Post office summary and any issues."
            )
        );
        
        report.AddGroup(ReportGroup.DistantElements, "Distant elements");

        report.AddEntry(
            ReportGroup.DistantElements,
            new DescriptionReportEntry(
                "Elements that are too far away from the average coord of their post code region (> " + distantElementThreshold + " meters)."
            )
        );
        
        report.AddEntry(
            ReportGroup.DistantElements,
            new PlaceholderReportEntry(
                "No elements are too far away from their post code region."
            )
        );
        
        // Parse
        
        // Find unique post codes
        // Also find/filter invalid ones

        Dictionary<string, List<OsmElement>> elementsByPostCode = new Dictionary<string, List<OsmElement>>();
        
        foreach (OsmElement postcodedElement in postcodedElements.Elements)
        {
            string postcode = postcodedElement.GetValue("addr:postcode")!;

            CodeValidation validation = ValidPostCodeSyntax(postcode, postcodedElement);

            if (validation != CodeValidation.Valid)
            {
                if (validation == CodeValidation.InvalidInLatvia)
                {
                    report.AddEntry(
                        ReportGroup.InvalidCodes,
                        new IssueReportEntry(
                            "Invalid post code `" + postcode + "` on " + postcodedElement.OsmViewUrl + ".",
                            postcodedElement.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }

                continue; // Skip this one
            }

            // Add to dictionary
            
            if (!elementsByPostCode.ContainsKey(postcode))
                elementsByPostCode[postcode] = new List<OsmElement>() { postcodedElement };
            else
                elementsByPostCode[postcode].Add(postcodedElement);
        }
        
        // Generic info about postcodes
        
        report.AddEntry(
            ReportGroup.Regions,
            new GenericReportEntry(
                "There are " + elementsByPostCode.Count + " unique post codes (for " + postcodedElements.Elements.Count + " addressable elements)."
            )
        );
        
        // Find the average coord of each unique postocode

        Dictionary<string, OsmCoord> averageCoords = new Dictionary<string, OsmCoord>();
        
        foreach ((string? postcode, List<OsmElement>? elements) in elementsByPostCode)
        {
            OsmCoord averageCoord = new OsmCoord(
                elements.Average(e => e.AverageCoord.lat),
                elements.Average(e => e.AverageCoord.lon)
            );
            
            averageCoords.Add(postcode, averageCoord);

            bool tooFew = elements.Count < 10;

            if (tooFew)
            {
                report.AddEntry(
                    ReportGroup.Regions,
                    new IssueReportEntry(
                        "Post code `" + postcode + "` region with only " + elements.Count + " addressable elements -- " + string.Join(", ", elements.Select(e => e.OsmViewUrl)),
                        new SortEntryAsc(postcode),
                        averageCoord,
                        MapPointStyle.Problem
                    )
                );
            }
            else
            {
                report.AddEntry(
                    ReportGroup.Regions,
                    new MapPointReportEntry(
                        averageCoord,
                        "Post code `" + postcode + "` region with " + elements.Count + " addressable elements.",
                        MapPointStyle.Okay
                    )
                );
            }
        }
        
        // Post offices
        
        Dictionary<string, OsmElement> postOfficesByPostCode = new Dictionary<string, OsmElement>();
        
        Dictionary<string, List<OsmElement>> repeatPostCodePostOffices = new Dictionary<string, List<OsmElement>>();

        foreach (OsmElement postOffice in postOffices.Elements)
        {
            string? postcode = postOffice.GetValue("addr:postcode");

            if (postcode == null)
            {
                report.AddEntry(
                    ReportGroup.PostOffices,
                    new IssueReportEntry(
                        "Post office " + postOffice.OsmViewUrl + " has no post code in/or address.",
                        postOffice.AverageCoord,
                        MapPointStyle.Problem
                    )
                );

                continue; // Skip further processing
            }

            if (ValidPostCodeSyntax(postcode, postOffice) != CodeValidation.Valid)
            {
                report.AddEntry(
                    ReportGroup.PostOffices,
                    new IssueReportEntry(
                        "Post office " + postOffice.OsmViewUrl + " has invalid post code `" + postcode + "`.",
                        new SortEntryAsc(postcode),
                        postOffice.AverageCoord,
                        MapPointStyle.Problem
                    )
                );

                continue; // Skip further processing
            }

            if (repeatPostCodePostOffices.ContainsKey(postcode))
            {
                // Add to repeat list
                repeatPostCodePostOffices[postcode].Add(postOffice);
            }
            else
            {
                // Check if we already have this post code
                if (postOfficesByPostCode.ContainsKey(postcode))
                {
                    // Move to repeat list
                    repeatPostCodePostOffices.Add(postcode, new List<OsmElement>() { postOfficesByPostCode[postcode], postOffice });
                    postOfficesByPostCode.Remove(postcode);
                }
                else
                {
                    // Add to main list
                    postOfficesByPostCode.Add(postcode, postOffice);
                }
            }
        }
        
        // Okay offices
        
        report.AddEntry(
            ReportGroup.PostOffices,
            new GenericReportEntry(
                "There are " + postOfficesByPostCode.Count + " post offices."
            )
        );
        
        foreach ((string postcode, OsmElement postOffice) in postOfficesByPostCode)
        {
            report.AddEntry(
                ReportGroup.PostOffices,
                new MapPointReportEntry(
                    postOffice.AverageCoord,
                    "Post office " + postOffice.OsmViewUrl + " for post code `" + postcode + "`.",
                    postOffice,
                    MapPointStyle.Okay
                )
            );
        }
        
        // Repeat post codes?
        
        foreach ((string postcode, List<OsmElement> reapeatPostOffices) in repeatPostCodePostOffices)
        {
            report.AddEntry(
                ReportGroup.PostOffices,
                new GenericReportEntry(
                    "Multiple post offices have the same post code in address `" + postcode + "` - " + string.Join(", ", reapeatPostOffices.Select(p => p.OsmViewUrl)),
                    new SortEntryAsc(postcode)
                )
            );
        }
        
        // Post office without elements?
        
        foreach ((string postcode, OsmElement postOffice) in postOfficesByPostCode)
        {
            if (!elementsByPostCode.ContainsKey(postcode))
            {
                report.AddEntry(
                    ReportGroup.PostOffices,
                    new IssueReportEntry(
                        "Post office " + postOffice.OsmViewUrl + " has a post code `" + postcode + "` that isn't used by any elements.",
                        new SortEntryAsc(postcode),
                        postOffice.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
            }
        }

        // Post code (region) with no post office, this is valid though
        
        Dictionary<string, List<OsmElement>> postCodesRegionsWithoutPostOffice = new Dictionary<string, List<OsmElement>>();
        
        foreach ((string postcode, List<OsmElement> elements) in elementsByPostCode)
            if (!postOfficesByPostCode.ContainsKey(postcode))
                postCodesRegionsWithoutPostOffice.Add(postcode, elements);

        if (postCodesRegionsWithoutPostOffice.Count > 0)
        {
            report.AddEntry(
                ReportGroup.PostOffices,
                new GenericReportEntry(
                    "There are " + postCodesRegionsWithoutPostOffice.Count + " post code regions without an exact post office - " + 
                    string.Join(", ", postCodesRegionsWithoutPostOffice.Keys.Order().Select(k => "`" + k + "`"))
                )
            );
        }

        // Find elements that are very far from their regions
        
        foreach ((string postcode, List<OsmElement> elements) in elementsByPostCode)
        {
            OsmCoord averageCoord = averageCoords[postcode];
            
            foreach (OsmElement element in elements)
            {
                double distance = OsmGeoTools.DistanceBetween(averageCoord, element.AverageCoord);
                
                if (distance > distantElementThreshold)
                {
                    report.AddEntry(
                        ReportGroup.DistantElements,
                        new IssueReportEntry(
                            "Element " + element.OsmViewUrl + " is too far away (" + distance + " meters) from the average coord of the post code region `" + postcode + "`.",
                            element.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }
        }
    }

    
    [Pure]
    private static CodeValidation ValidPostCodeSyntax(string postcode, OsmElement element)
    {
        // Must be "LV-####"
        if (Regex.IsMatch(postcode, @"^LV-\d{4}$"))
            return CodeValidation.Valid;
        
        // Could be not in Latvia (i.e. not addr:country=LV)
        string? addrCountry = element.GetValue("addr:country");
        
        if (addrCountry != null && addrCountry != "LV")
            return CodeValidation.NotInLatvia; // implicitly belongs to another country by address, so we don't care

        if (!_latviaPolygon.ContainsElement(element, OsmPolygon.RelationInclusionCheck.FuzzyLoose))
            return CodeValidation.NotInLatvia; // outside exact admin border, so most likely belongs to another country, so we don't care

        return CodeValidation.InvalidInLatvia; // we couldn't rule this out as valid or non-Latvia, so invalid
    }


    private enum CodeValidation
    {
        Valid,
        InvalidInLatvia,
        NotInLatvia
    }

        
    private enum ReportGroup
    {
        Regions,
        InvalidCodes,
        DistantElements,
        PostOffices
    }
}