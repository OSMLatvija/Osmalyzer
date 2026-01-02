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
        typeof(AtvkAnalysisData)
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
                                           .Where(e => !e.IsExpired && e.Designation is AtkvDesignation.StateCity or AtkvDesignation.RegionalCity).ToList();

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
                if (name is "Daugavpils" or "Jelgava" or "Jēkabpils" or "Jūrmala" or "Liepāja" or "Ogre" or "Rēzekne" or "Rīga" or "Valmiera" or "Ventspils")
                    return true;
                
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
        
        // Match VZD and ATVK data items

        Equivalator<City, AtkvEntry> equivalator = new Equivalator<City, AtkvEntry>(
            addressData.Cities, 
            atvkEntries
        );
        
        equivalator.MatchItemsByValues(
            c => c.Name,
            e => e.Name
        );
        
        Dictionary<City, AtkvEntry> dataItemMatches = equivalator.AsDictionary();
        if (dataItemMatches.Count == 0) throw new Exception("No VZD-ATVK matches found for data items; data is probably broken.");
        
        // Validate city syntax
        
        Validator<City> cityValidator = new Validator<City>(
            cityCorrelation,
            "City syntax issues"
        );

        List<SuggestedAction> suggestedChanges = cityValidator.Validate(
            report,
            false,
            new ValidateElementHasValue("place", "city"),
            new ValidateElementValueMatchesDataItemValue<City>("ref:LV:addr", c => c.ID, [ "ref" ]),
            new ValidateElementValueMatchesDataItemValue<City>("ref", c => dataItemMatches.TryGetValue(c, out AtkvEntry? match) ? match.Code : null),
            new ValidateElementValueMatchesDataItemValue<City>("ref:nuts", c => dataItemMatches.TryGetValue(c, out AtkvEntry? match) ? match.Code : null)
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
    }


    private enum ExtraReportGroup
    {
        CityBoundaries,
        InvalidCities
    }
}

