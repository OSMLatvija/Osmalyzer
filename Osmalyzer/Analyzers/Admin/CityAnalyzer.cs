using System.Diagnostics;
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
        typeof(StateCitiesAnalysisData),
        typeof(VdbAnalysisData)
    ];

    
    private const string independentStateCityAdminLevel = "5"; // valstspilsēta on the same level as municipalities
    private const string dependentStateCityAdminLevel = "7"; // valstspilsēta within its municipality, on the same level as regional cities
    private const string regionalCityAdminLevel = "7"; // all other pilsēta within its municipality


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;


        OsmDataExtract osmCities = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasAnyValue("admin_level", independentStateCityAdminLevel, dependentStateCityAdminLevel, regionalCityAdminLevel), // 5 - valstspilsētas, 7 - pilsētas
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside) // lots around edges
        );
        
        // todo: get place=city and place=town and also check no others are tagged as such
        
        // Find preset centers of boundaries
        // (we won't need to set some values if there is the "master" node for the relation)

        foreach (OsmRelation relation in osmCities.Relations)
        {
            List<OsmRelationMember> knownCenters = relation.Members.Where(m => m.Role == "admin_centre" && m.Element != null).ToList();

            if (knownCenters.Count == 1) // todo: else report
                relation.UserData = knownCenters[0].Element;

            if (knownCenters.Count == 0)
            {
                List<OsmRelationMember> labelCenters = relation.Members.Where(m => m.Role == "label" && m.Element != null).ToList();
                
                if (labelCenters.Count == 1)
                    relation.UserData = labelCenters[0].Element; // label is fine too
                // todo: do we need to check values like place= on it to make sure it's actually representing the center?
            }
        }

        // Get city data

        AddressGeodataAnalysisData addressData = datas.OfType<AddressGeodataAnalysisData>().First();

        List<AtvkEntry> atvkEntries = datas.OfType<AtvkAnalysisData>().First().Entries
                                           .Where(e => !e.IsExpired && e.Designation is AtvkDesignation.CityInRegion or AtvkDesignation.CityInMunicipality).ToList();

        CitiesWikidataData wikidataData = datas.OfType<CitiesWikidataData>().First();
        
        StateCitiesAnalysisData stateCitiesData = datas.OfType<StateCitiesAnalysisData>().First();
        
        VdbAnalysisData vdbData = datas.OfType<VdbAnalysisData>().First();
        
        // Match VZD and ATVK data items

        Equivalator<City, AtvkEntry> equivalator = new Equivalator<City, AtvkEntry>(
            addressData.Cities, 
            atvkEntries
        );
        
        equivalator.MatchItems(
            (i1, i2) => i1.Name == i2.Name // we have no name conflicts in cities, so this is sufficient
        );
        
        Dictionary<City, AtvkEntry> dataItemMatches = equivalator.AsDictionary();
        if (dataItemMatches.Count == 0) throw new Exception("No VZD-ATVK matches found for data items; data is probably broken.");

        foreach ((City city, AtvkEntry atvkEntry) in dataItemMatches)
        {
            // Cities that are not part of a municipality (i.e. are under region) are LAU divisions
            city.IsLAUDivision = atvkEntry.Parent?.Designation == AtvkDesignation.Region;

            KnownStateCity? knownStateCity = stateCitiesData.StateCities.FirstOrDefault(sc => sc.Name == city.Name);

            city.Status = knownStateCity != null ? CityStatus.StateCity : CityStatus.RegionalCity;
            
            if (knownStateCity != null)
                city.IndependentStateCity = knownStateCity.IndependentOfMunicipality;
        }
        
        // Assign WikiData
        
        wikidataData.Assign(
            addressData.Cities,
            (i, wd) => i.Name == wd.GetBestName("lv"), // we have no name conflicts in cities, so this is sufficient
            30000, 
            out List<WikidataData.WikidataMatchIssue> wikidataMatchIssues
        );

        // Assign VDB data

        vdbData.AssignToDataItems(
            addressData.Cities,
            (i, vdb) =>
                i.Name == vdb.Name &&
                vdb.ObjectType is VdbEntryObjectType.StateCity or VdbEntryObjectType.MunicipalCities,
            30000,
            out List<VdbMatchIssue> vdbMatchIssues
        );
        
        // Prepare data comparer/correlator

        Correlator<City> cityCorrelator = new Correlator<City>(
            osmCities,
            addressData.Cities,
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
            if (adminLevel == regionalCityAdminLevel)
            {
                string? name = element.GetValue("name");
                if (name != null && name.EndsWith(" vald")) // Estonian towns leaking
                    return false;
                
                return true; // city admin level
            }

            if (adminLevel is independentStateCityAdminLevel or dependentStateCityAdminLevel)
            {
                string? name = element.GetValue("name");
                if (name != null && stateCitiesData.StateCities.Any(sc => sc.Name == name))
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
            false, false,
            // On relation itself
            new ValidateElementValueMatchesDataItemValue<City>("border_type", GetPlaceType),
            new ValidateElementValueMatchesDataItemValue<City>("admin_level", c => c.Status == CityStatus.StateCity ? c.IndependentStateCity ? independentStateCityAdminLevel : dependentStateCityAdminLevel : regionalCityAdminLevel),
            new ValidateElementValueMatchesDataItemValue<City>("ref", c => dataItemMatches.TryGetValue(c, out AtvkEntry? match) ? match.Code : null),
            new ValidateElementValueMatchesDataItemValue<City>("ref:lau", c => c.IsLAUDivision == true ? dataItemMatches.TryGetValue(c, out AtvkEntry? match) ? match.Code : "" : null, [ "ref:nuts" ]),
            new ValidateElementValueMatchesDataItemValue<City>("ref:LV:addr", c => c.AddressID, [ "ref" ]),
            // If no admin center given, check tags directly on relation
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData == null, "place", GetPlaceType),
            new ValidateElementDoesntHaveTag(e => e.UserData != null, "place"),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData == null, "wikidata", c => c.WikidataItem?.QID),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData == null, "designation", c => c.Status == CityStatus.StateCity ? "valstspilsēta" : null),
            // If admin center given, check tags on the admin center node
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData != null, e => (OsmElement)e.UserData!, "place", GetPlaceType),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData != null, e => (OsmElement)e.UserData!, "wikidata", c => c.WikidataItem?.QID),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData != null, e => (OsmElement)e.UserData!, "designation", c => c.Status == CityStatus.StateCity ? "valstspilsēta" : null)
        );

        string GetPlaceType(City c) => c.Status == CityStatus.StateCity ? "city" : "town"; // apparently, regional cities are place=town in Latvia atm

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this);
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif
        
        // List invalid cities that are still in data
        
        report.AddGroup(
            ExtraReportGroup.InvalidCities,
            "Invalid Cities",
            "Cities marked invalid in address geodata (not approved or not existing).",
            "There are no invalid cities in the geodata."
        );

        foreach (City city in addressData.InvalidCities)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidCities,
                new IssueReportEntry(
                    city.ReportString()
                )
            );
        }
        
        // Check that Wikidata values match OSM values
        
        // TODO:
        // TODO:
        // TODO:
        
        // List extra data items from non-OSM that were not matched
        
        report.AddGroup(
            ExtraReportGroup.ExternalDataMatchingIssues,
            "Extra data item matching issues",
            "This section lists any issues with data item matching to additional external data sources.",
            "No issues found."
        );
        
        List<AtvkEntry> extraAtvkEntries = atvkEntries
            .Where(e => !dataItemMatches.Values.Contains(e))
            .ToList();
        
        foreach (AtvkEntry atvkEntry in extraAtvkEntries)
        {
            report.AddEntry(
                ExtraReportGroup.ExternalDataMatchingIssues,
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
            string? name = wikidataItem.GetBestName("lv") ?? null;

            report.AddEntry(
                ExtraReportGroup.ExternalDataMatchingIssues,
                new IssueReportEntry(
                    "Wikidata city item " + wikidataItem.WikidataUrl + (name != null ? " `" + name + "` " : "") + " was not matched to any OSM element."
                )
            );
        }
        
        foreach (WikidataData.WikidataMatchIssue matchIssue in wikidataMatchIssues)
        {
            switch (matchIssue)
            {
                case WikidataData.MultipleWikidataMatchesWikidataMatchIssue<City> multipleWikidataMatches:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            multipleWikidataMatches.DataItem.ReportString() + " matched multiple Wikidata items: " +
                            string.Join(", ", multipleWikidataMatches.WikidataItems.Select(wd => wd.WikidataUrl))
                        )
                    );
                    break;
                
                case WikidataData.CoordinateMismatchWikidataMatchIssue<City> coordinateMismatch:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            coordinateMismatch.DataItem.ReportString() + " matched a Wikidata item, but the Wikidata coordinate is too far at " +
                            coordinateMismatch.DistanceMeters.ToString("F0") + " m" +
                            " -- " + coordinateMismatch.WikidataItem.WikidataUrl
                        )
                    );
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchIssue));
            }
        }

        foreach (VdbMatchIssue vdbMatchIssue in vdbMatchIssues)
        {
            switch (vdbMatchIssue)
            {
                case MultipleVdbMatchesVdbMatchIssue<City> multipleVdbMatches:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            multipleVdbMatches.DataItem.ReportString() + " matched multiple VDB entries: " +
                            string.Join(", ", multipleVdbMatches.VdbEntries.Select(vdb => vdb.ReportString()))
                        )
                    );
                    break;
                
                case CoordinateMismatchVdbMatchIssue<City> coordinateMismatch:
                    report.AddEntry(
                        ExtraReportGroup.ExternalDataMatchingIssues,
                        new IssueReportEntry(
                            coordinateMismatch.DataItem.ReportString() + " matched a VDB entry, but the VDB coordinate is too far at " +
                            coordinateMismatch.DistanceMeters.ToString("F0") + " m" +
                            " -- " + coordinateMismatch.VdbEntry.ReportString()
                        )
                    );
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(vdbMatchIssue));
            }
        }

        foreach (City city in addressData.Cities)
        {
            if (city.WikidataItem == null)
            {
                report.AddEntry(
                    ExtraReportGroup.ExternalDataMatchingIssues,
                    new IssueReportEntry(
                        city.ReportString() + " does not have a matched Wikidata item."
                    )
                );
            }
            
            if (city.VdbEntry == null)
            {
                report.AddEntry(
                    ExtraReportGroup.ExternalDataMatchingIssues,
                    new IssueReportEntry(
                        city.ReportString() + " does not have a matched VDB entry."
                    )
                );
            }
        }
    }


    private enum ExtraReportGroup
    {
        CityBoundaries,
        InvalidCities,
        ExternalDataMatchingIssues,
        ProposedChanges
    }
}

