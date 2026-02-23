namespace Osmalyzer;

[UsedImplicitly]
public class CityAnalyzer : AdminAnalyzerBase<City>
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
        typeof(VdbAnalysisData),
        typeof(CspPopulationAnalysisData)
    ];

    
    private const string independentStateCityAdminLevel = "5"; // valstspilsēta on the same level as municipalities
    private const string dependentStateCityAdminLevel = "7"; // valstspilsēta within its municipality, on the same level as regional cities
    private const string regionalCityAdminLevel = "7"; // all other pilsēta within its municipality


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmData OsmData = osmData.MasterData;


        OsmData osmCities = OsmData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasAnyValue("admin_level", independentStateCityAdminLevel, dependentStateCityAdminLevel, regionalCityAdminLevel), // 5 - valstspilsētas, 7 - pilsētas
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside) // lots around edges
        );
        
        // todo: get place=city and place=town and also check no others are tagged as such
        
        // Find preset centers of boundaries
        // (we won't need to set some values if there is the "master" node for the relation)

        SelfAssignAdminCenters(osmCities.Relations);

        // Get all data sources

        AddressGeodataAnalysisData addressData = datas.OfType<AddressGeodataAnalysisData>().First();

        AtvkAnalysisData atvkData = datas.OfType<AtvkAnalysisData>().First();

        List<AtvkEntry> atvkEntries = atvkData.Entries
                                           .Where(e => !e.IsExpired && e.Designation is AtvkDesignation.CityInRegion or AtvkDesignation.CityInMunicipality).ToList();

        CitiesWikidataData wikidataData = datas.OfType<CitiesWikidataData>().First();
        
        StateCitiesAnalysisData stateCitiesData = datas.OfType<StateCitiesAnalysisData>().First();
        
        VdbAnalysisData vdbData = datas.OfType<VdbAnalysisData>().First();
        
        CspPopulationAnalysisData cspData = datas.OfType<CspPopulationAnalysisData>().First();
        
        // Match VZD and ATVK data items

        atvkData.AssignToDataItems(
            addressData.Cities,
            atvkEntries,
            (city, atvkEntry) => city.Name == atvkEntry.Name // we have no name conflicts in cities, so this is sufficient
        );

        foreach (City city in addressData.Cities)
        {
            if (city.AtvkEntry == null)
                continue;

            AtvkEntry atvkEntry = city.AtvkEntry;

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
            vdbData.Cities,
            i => i.Name,
            null,
            null,
            30000,
            2000,
            out List<VdbMatchIssue> vdbMatchIssues
        );
        
        // Assign CSP population data
        
        cspData.AssignToDataItems(
            addressData.Cities,
            CspAreaType.City,
            i => i.Name,
            _ => null, // not doing lookups by code
            _ => null // none should need it
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
            string? refAddr = osmElement.GetValue("ref:LV:addr");
            
            if (refAddr == city.AddressID)
                return MatchStrength.Strong; // exact match on address id (presumably previously-imported and assumed correct)
            
            string? name = osmElement.GetValue("name");

            if (name == city.Name)
                return MatchStrength.Strong; // exact match on name
            
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
            // Always on relation itself
            new ValidateElementValueMatchesDataItemValue<City>("name", c => c.Name),
            new ValidateElementValueMatchesDataItemValue<City>("border_type", GetPlaceType),
            new ValidateElementValueMatchesDataItemValue<City>("admin_level", c => c.Status == CityStatus.StateCity ? c.IndependentStateCity ? independentStateCityAdminLevel : dependentStateCityAdminLevel : regionalCityAdminLevel),
            new ValidateElementValueMatchesDataItemValue<City>("ref", c => c.AtvkEntry?.Code),
            new ValidateElementValueMatchesDataItemValue<City>("ref:lau", c => c.IsLAUDivision == true ? c.AtvkEntry?.Code ?? "" : null, [ "ref:nuts" ]),
            new ValidateElementValueMatchesDataItemValue<City>("ref:LV:addr", c => c.AddressID, [ "ref" ]),
            // Always on admin center node if given
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData != null, e => (OsmElement)e.UserData!, "name", c => c.Name),
            // If no admin center given, check tags directly on relation
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData == null, "place", GetPlaceType),
            new ValidateElementDoesntHaveTag(e => e.UserData != null, "place"),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData == null, "wikidata", c => c.WikidataItem?.QID),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData == null, "designation", c => c.Status == CityStatus.StateCity ? "valstspilsēta" : "novada pilsēta"),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData == null, "ref:LV:VDB", c => c.VdbEntry?.ID.ToString()),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData == null, "population", c => c.CspPopulationEntry?.Population.ToString()),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData == null, "source:population", c => c.CspPopulationEntry?.Source),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData == null, "population:date", c => c.CspPopulationEntry?.Year.ToString()),
            // If admin center given, check tags on the admin center node
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData != null, e => (OsmElement)e.UserData!, "place", GetPlaceType),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData != null, e => (OsmElement)e.UserData!, "wikidata", c => c.WikidataItem?.QID),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData != null, e => (OsmElement)e.UserData!, "designation", c => c.Status == CityStatus.StateCity ? "valstspilsēta" : "novada pilsēta"),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData != null, e => (OsmElement)e.UserData!, "ref:LV:VDB", c => c.VdbEntry?.ID.ToString()),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData != null, e => (OsmElement)e.UserData!, "population", c => c.CspPopulationEntry?.Population.ToString()),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData != null, e => (OsmElement)e.UserData!, "source:population", c => c.CspPopulationEntry?.Source),
            new ValidateElementValueMatchesDataItemValue<City>(e => e.UserData != null, e => (OsmElement)e.UserData!, "population:date", c => c.CspPopulationEntry?.Year.ToString())
        );

        string GetPlaceType(City c) => c.Status == CityStatus.StateCity ? "city" : "town"; // apparently, regional cities are place=town in Latvia atm

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(OsmData, suggestedChanges, this);
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
        
        // List extrenal data items issues
        
        AddExternalDataMatchingIssuesGroup(report, ExtraReportGroup.ExternalDataMatchingIssues);
        
        ReportExtraAtvkEntries(report, ExtraReportGroup.ExternalDataMatchingIssues, atvkEntries, addressData.Cities, "city");
        ReportExtraWikidataItems(report, ExtraReportGroup.ExternalDataMatchingIssues, wikidataData.AllCities, addressData.Cities, "city");
        ReportWikidataMatchIssues(report, ExtraReportGroup.ExternalDataMatchingIssues, wikidataMatchIssues);
        ReportVdbMatchIssues(report, ExtraReportGroup.ExternalDataMatchingIssues, vdbMatchIssues);
        ReportMissingWikidataItems(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Cities);
        ReportMissingVdbEntries(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Cities, vdbData.Cities);
        ReportUnmatchedOsmWikidataValues(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Cities, cityCorrelation);
        ReportMissingCspPopulationEntries(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Cities);
    }


    private enum ExtraReportGroup
    {
        CityBoundaries,
        InvalidCities,
        ExternalDataMatchingIssues,
        ProposedChanges
    }
}

