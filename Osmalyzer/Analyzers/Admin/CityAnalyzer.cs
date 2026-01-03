using WikidataSharp;

namespace Osmalyzer;

[UsedImplicitly]
public class CityAnalyzer : Analyzer
{
    public override string Name => "Cities";

    public override string Description => 
        "This report checks that all cities are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ 
        typeof(LatviaOsmAnalysisData), 
        typeof(AddressGeodataAnalysisData),
        typeof(AtvkAnalysisData),
        typeof(CitiesWikidataData),
        typeof(StateCitiesAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmCities = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasAnyValue("admin_level", "5", "7"), // 5 - valstspilsētas, 7 - pilsētas
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside) // lots around edges
        );

        // Get city data

        AddressGeodataAnalysisData addressData = datas.OfType<AddressGeodataAnalysisData>().First();

        List<AtkvEntry> atvkEntries = datas.OfType<AtvkAnalysisData>().First().Entries
                                           .Where(e => !e.IsExpired && e.Designation is AtkvDesignation.CityInRegion or AtkvDesignation.CityInMunicipality).ToList();

        CitiesWikidataData wikidataData = datas.OfType<CitiesWikidataData>().First();
        
        StateCitiesAnalysisData stateCitiesData = datas.OfType<StateCitiesAnalysisData>().First();
        
        // Match VZD and ATVK data items

        Equivalator<City, AtkvEntry> equivalator = new Equivalator<City, AtkvEntry>(
            addressData.Cities, 
            atvkEntries
        );
        
        equivalator.MatchItems(
            (i1, i2) => i1.Name == i2.Name // we have no name conflicts in cities, so this is sufficient
        );
        
        Dictionary<City, AtkvEntry> dataItemMatches = equivalator.AsDictionary();
        if (dataItemMatches.Count == 0) throw new Exception("No VZD-ATVK matches found for data items; data is probably broken.");

        foreach ((City city, AtkvEntry atkvEntry) in dataItemMatches)
        {
            // Cities that are not part of a municipality (i.e. are under region) are LAU divisions
            city.IsLAUDivision = atkvEntry.Parent?.Designation == AtkvDesignation.Region;
            
            city.Status = stateCitiesData.Names.Contains(city.Name) ? CityStatus.StateCity : CityStatus.RegionalCity;
        }
        
        // Assign WikiData
        
        wikidataData.Assign(
            addressData.Cities,
            (i, wd) => i.Name == AdminWikidataData.GetBestName(wd, "lv"), // we have no name conflicts in cities, so this is sufficient
            out List<(City, List<WikidataItem>)> multiMatches
        );
        
        // Prepare data comparer/correlator

        Correlator<City> cityCorrelator = new Correlator<City>(
            osmCities,
            addressData.Cities.Where(c => c.Valid).ToList(),
            new MatchDistanceParamater(10000),
            new MatchFarDistanceParamater(30000),
            new MatchCallbackParameter<City>(GetCityMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("city", "cities"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeACity)
        );

        [Pure]
        MatchStrength GetCityMatchStrength(City city, OsmElement osmElement)
        {
            string? name = osmElement.GetValue("name");

            if (name == city.Name)
                return MatchStrength.Strong; // exact match on name

            if (DoesOsmElementLookLikeACity(osmElement))
                return MatchStrength.Good; // looks like a city, but not exact match
            
            return MatchStrength.Unmatched;
        }

        [Pure]
        bool DoesOsmElementLookLikeACity(OsmElement element)
        {
            string? place = element.GetValue("place");
            if (place == "city")
                return true; // explicitly tagged
            if (place != null)
                return false; // other place type tagged
            
            string adminLevel = element.GetValue("admin_level")!;
            if (adminLevel == "7")
            {
                string? name = element.GetValue("name");
                if (name != null && name.EndsWith(" vald")) // Estonian towns leaking
                    return false;
                
                return true; // city admin level
            }

            if (adminLevel == "5")
            {
                string? name = element.GetValue("name");
                if (name != null && stateCitiesData.Names.Contains(name))
                    return true; // state cities have high admin level shared with municipalities, but we know them
                
                if (name != null && name.Contains("rajono"))
                    return false; // Lithuanian district
            }

            return true;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport cityCorrelation = cityCorrelator.Parse(
            report, 
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        // Validate city boundaries
        
        const double matchLimit = 0.99;
        
        report.AddGroup(
            ExtraReportGroup.CityBoundaries,
            "City boundary issues",
            "This section lists cities where the mapped boundary does not sufficiently cover the official boundary polygon. " +
            "Due to data fuzziness, small mismatches are expected and not reported (" + (matchLimit * 100).ToString("F1") + "% coverage required)."
        );

        foreach (Correlation correlation in cityCorrelation.Correlations)
        {
            if (correlation is MatchedCorrelation<City> matchedCorrelation)
            {
                City city = matchedCorrelation.DataItem;
                OsmElement osmElement = matchedCorrelation.OsmElement;

                if (osmElement is OsmRelation relation)
                {
                    OsmMultiPolygon? relationMultiPolygon = relation.GetMultipolygon();
                    
                    if (relationMultiPolygon == null)
                    {
                        report.AddEntry(
                            ExtraReportGroup.CityBoundaries,
                            new IssueReportEntry(
                                "City relation for `" + city.Name + "` does not have a valid polygon for " + osmElement.OsmViewUrl,
                                osmElement.AverageCoord,
                                MapPointStyle.Problem,
                                osmElement
                            )
                        );
                        
                        continue;
                    }
                    
                    OsmMultiPolygon cityBoundary = city.Boundary!;

                    double estimatedCoverage = cityBoundary.GetOverlapCoveragePercent(relationMultiPolygon, 50);

                    if (estimatedCoverage < matchLimit)
                    {
                        report.AddEntry(
                            ExtraReportGroup.CityBoundaries,
                            new IssueReportEntry(
                                "City boundary for `" + city.Name + "` does not match the official boundary polygon " +
                                "(matches at " + (estimatedCoverage * 100).ToString("F1") + "%) for " + osmElement.OsmViewUrl,
                                new SortEntryAsc(estimatedCoverage),
                                osmElement.AverageCoord,
                                estimatedCoverage < 0.95 ? MapPointStyle.Problem : MapPointStyle.Dubious,
                                osmElement
                            )
                        );
                    }
                }
            }
        }
        
        // Validate city syntax
        
        Validator<City> cityValidator = new Validator<City>(
            cityCorrelation,
            "City syntax issues"
        );

        List<SuggestedAction> suggestedChanges = cityValidator.Validate(
            report,
            false,
            new ValidateElementHasValue("place", "city"),
            new ValidateElementValueMatchesDataItemValue<City>("ref", c => dataItemMatches.TryGetValue(c, out AtkvEntry? match) ? match.Code : null),
            new ValidateElementValueMatchesDataItemValue<City>("ref:lau", c => c.IsLAUDivision == true ? dataItemMatches.TryGetValue(c, out AtkvEntry? match) ? match.Code : "" : null, [ "ref:nuts" ]),
            new ValidateElementValueMatchesDataItemValue<City>("ref:LV:addr", c => c.AddressID, [ "ref" ]),
            new ValidateElementValueMatchesDataItemValue<City>("wikidata", c => c.WikidataItem?.QID),
            new ValidateElementValueMatchesDataItemValue<City>("designation", c => c.Status == CityStatus.StateCity ? "valstspilsēta" : null)
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this);
#endif
        
        // List invalid cities that are still in data
        
        report.AddGroup(
            ExtraReportGroup.InvalidCities,
            "Invalid Cities",
            "Cities marked invalid in address geodata (not approved or not existing).",
            "There are no invalid cities in the geodata."
        );

        List<City> invalidCities = addressData.Cities.Where(c => !c.Valid).ToList();

        foreach (City city in invalidCities)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidCities,
                new IssueReportEntry(
                    city.ReportString()
                )
            );
        }
        
        // List extra data items from non-OSM that were not matched
        
        report.AddGroup(
            ExtraReportGroup.ExtraDataItems,
            "Extra data items",
            "This section lists data items from additional external data sources that were not matched to any OSM element.",
            "All external data items were matched to OSM elements."
        );
        
        List<AtkvEntry> extraAtvkEntries = atvkEntries
            .Where(e => !dataItemMatches.Values.Contains(e))
            .ToList();
        
        foreach (AtkvEntry atvkEntry in extraAtvkEntries)
        {
            report.AddEntry(
                ExtraReportGroup.ExtraDataItems,
                new IssueReportEntry(
                    "ATVK entry for city `" + atvkEntry.Name + "` (#`" + atvkEntry.Code + "`) was not matched to any OSM element."
                )
            );
        }
        
        List<WikidataItem> extraWikidataItems = wikidataData.AllCities
            .Where(wd => addressData.Cities.All(c => c.WikidataItem != wd))
            .ToList();

        foreach (WikidataItem wikidataItem in extraWikidataItems)
        {
            string? name = AdminWikidataData.GetBestName(wikidataItem, "lv") ?? null;

            report.AddEntry(
                ExtraReportGroup.ExtraDataItems,
                new IssueReportEntry(
                    "Wikidata city item " + wikidataItem.WikidataUrl + (name != null ? " `" + name + "` " : "") + " was not matched to any OSM element."
                )
            );
        }
        
        foreach ((City city, List<WikidataItem> matches) in multiMatches)
        {
            report.AddEntry(
                ExtraReportGroup.ExtraDataItems,
                new IssueReportEntry(
                    city.ReportString() + " matched multiple Wikidata items: " +
                    string.Join(", ", matches.Select(wd => wd.WikidataUrl))
                )
            );
        }
    }


    private enum ExtraReportGroup
    {
        CityBoundaries,
        InvalidCities,
        ExtraDataItems
    }
}

