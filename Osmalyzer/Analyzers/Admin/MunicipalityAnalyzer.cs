namespace Osmalyzer;

[UsedImplicitly]
public class MunicipalityAnalyzer : AdminAnalyzerBase<Municipality>
{
    public override string Name => "Municipalities";

    public override string Description => 
        "This report checks that all municipalities are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ 
        typeof(LatviaOsmAnalysisData), 
        typeof(AddressGeodataAnalysisData),
        typeof(AtvkAnalysisData),
        typeof(MunicipalitiesWikidataData),
        typeof(StateCitiesAnalysisData),
        typeof(VdbAnalysisData),
        typeof(CspPopulationAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmData OsmData = osmData.MasterData;

        OsmData osmMunicipalities = OsmData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasAnyValue("admin_level", "5"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside) // lots around edges
        );

        // Get all data sources

        AddressGeodataAnalysisData addressData = datas.OfType<AddressGeodataAnalysisData>().First();

        AtvkAnalysisData atvkData = datas.OfType<AtvkAnalysisData>().First();

        List<AtvkEntry> atvkEntries = atvkData.Entries
                                           .Where(e => !e.IsExpired && e.Designation == AtvkDesignation.Municipality).ToList();
        
        MunicipalitiesWikidataData wikidataData = datas.OfType<MunicipalitiesWikidataData>().First();
        
        StateCitiesAnalysisData stateCitiesData = datas.OfType<StateCitiesAnalysisData>().First();
        
        VdbAnalysisData vdbData = datas.OfType<VdbAnalysisData>().First();
        
        CspPopulationAnalysisData cspData = datas.OfType<CspPopulationAnalysisData>().First();
        
        // Match VZD and ATVK data items

        atvkData.AssignToDataItems(
            addressData.Municipalities,
            atvkEntries,
            (municipality, atvkEntry) => municipality.Name == atvkEntry.Name // we have no name conflicts in municipalities, so this is sufficient
        );
        
        // Assign WikiData
        
        wikidataData.Assign(
            addressData.Municipalities,
            (i, wd) => i.Name == wd.GetBestName("lv"), // we have no name conflicts in municipalities, so this is sufficient
            75000,
            out List<WikidataData.WikidataMatchIssue> wikidataMatchIssues
        );

        // Assign VDB data

        vdbData.AssignToDataItems(
            addressData.Municipalities,
            vdbData.Municipalities,
            i => i.Name,
            null,
            null,
            75000,
            5000,
            out List<VdbMatchIssue> vdbMatchIssues
        );
        
        // Assign CSP population data
        
        cspData.AssignToDataItems(
            addressData.Municipalities,
            CspAreaType.Municipality,
            i => i.Name,
            _ => null // none should need it
        );

        // Prepare data comparer/correlator

        Correlator<Municipality> municipalityCorrelator = new Correlator<Municipality>(
            osmMunicipalities,
            addressData.Municipalities,
            new MatchDistanceParamater(25000),
            new MatchFarDistanceParamater(75000),
            new MatchCallbackParameter<Municipality>(GetMunicipalityMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("municipality", "municipalities"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeAMunicipality)
        );

        [Pure]
        MatchStrength GetMunicipalityMatchStrength(Municipality municipality, OsmElement osmElement)
        {
            string? refAddr = osmElement.GetValue("ref:LV:addr");
            
            if (refAddr == municipality.AddressID)
                return MatchStrength.Strong; // exact match on address id (presumably previously-imported and assumed correct)

            string? name = osmElement.GetValue("name");

            if (name == municipality.Name)
                return MatchStrength.Strong; // exact match on name
            
            return MatchStrength.Unmatched;
        }

        [Pure]
        bool DoesOsmElementLookLikeAMunicipality(OsmElement element)
        {
            string? place = element.GetValue("place");
            if (place == "municipality")
                return true; // explicitly tagged
            
            if (place == "city")
                return false; // explicitly not tagged
            
            string? name = element.GetValue("name");
            if (name != null && stateCitiesData.StateCities.Any(sc => sc.Name == name))
                return false; // state cities have the same admin level as municipalities, but we know them, so we can exclude them
                        
            if (name != null && name.Contains("rajono"))
                return false; // Lithuanian district
            
            return true;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport municipalityCorrelation = municipalityCorrelator.Parse(
            report, 
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        // Validate municipality boundaries
        
        const double matchLimit = 0.99;
        
        report.AddGroup(
            ExtraReportGroup.MunicipalityBoundaries,
            "Municipality boundary issues",
            "This section lists municipalities where the mapped boundary does not sufficiently cover the official boundary polygon. " +
            "Due to data fuzziness, small mismatches are expected and not reported (" + (matchLimit * 100).ToString("F1") + "% coverage required)."
        );

        foreach (Correlation correlation in municipalityCorrelation.Correlations)
        {
            if (correlation is MatchedCorrelation<Municipality> matchedCorrelation)
            {
                Municipality municipality = matchedCorrelation.DataItem;
                OsmElement osmElement = matchedCorrelation.OsmElement;

                if (osmElement is OsmRelation relation)
                {
                    OsmMultiPolygon? relationMultiPolygon = relation.GetMultipolygon();
                    
                    if (relationMultiPolygon == null)
                    {
                        report.AddEntry(
                            ExtraReportGroup.MunicipalityBoundaries,
                            new IssueReportEntry(
                                "Municipality relation does not have a valid polygon for " + osmElement.OsmViewUrl,
                                osmElement.AverageCoord,
                                MapPointStyle.Problem,
                                osmElement
                            )
                        );
                        
                        continue;
                    }
                    
                    OsmMultiPolygon municipalityBoundary = municipality.Boundary!;

                    double estimatedCoverage = municipalityBoundary.GetOverlapCoveragePercent(relationMultiPolygon, 50);

                    if (estimatedCoverage < matchLimit)
                    {
                        report.AddEntry(
                            ExtraReportGroup.MunicipalityBoundaries,
                            new IssueReportEntry(
                                "Municipality boundary for `" + municipality.Name + "` does not match the official boundary polygon " +
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
        
        // Validate municipality syntax
        
        Validator<Municipality> municipalityValidator = new Validator<Municipality>(
            municipalityCorrelation,
            "Municipality syntax issues"
        );

        List<SuggestedAction> suggestedChanges = municipalityValidator.Validate(
            report,
            false, false,
            new ValidateElementValueMatchesDataItemValue<Municipality>("name", m => m.Name),
            new ValidateElementHasValue("place", "municipality"),
            new ValidateElementHasValue("border_type", "municipality"),
            new ValidateElementValueMatchesDataItemValue<Municipality>("ref:LV:addr", m => m.AddressID, [ "ref" ]),
            new ValidateElementValueMatchesDataItemValue<Municipality>("ref", m => m.AtvkEntry?.Code),
            new ValidateElementValueMatchesDataItemValue<Municipality>("ref:lau", m => m.AtvkEntry?.Code),
            new ValidateElementValueMatchesDataItemValue<Municipality>("wikidata", m => m.WikidataItem?.QID),
            new ValidateElementValueMatchesDataItemValue<Municipality>("ref:LV:VDB", m => m.VdbEntry?.ID.ToString()),
            new ValidateElementValueMatchesDataItemValue<Municipality>(e => e.UserData == null, "population:date", c => c.CspPopulationEntry?.Population.ToString()),
            new ValidateElementValueMatchesDataItemValue<Municipality>(e => e.UserData == null, "source:population", c => c.CspPopulationEntry?.Source)
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(OsmData, suggestedChanges, this);
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif

        // List invalid municipalities that are still in data
        
        report.AddGroup(
            ExtraReportGroup.InvalidMunicipalities,
            "Invalid Municipalities",
            "Municipalities marked invalid in address geodata (not approved or not existing).",
            "There are no invalid municipalities in the geodata."
        );

        foreach (Municipality municipality in addressData.InvalidMunicipalities)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidMunicipalities,
                new IssueReportEntry(
                    municipality.ReportString()
                )
            );
        }
        
        // List extra data items from non-OSM that were not matched
        
        AddExternalDataMatchingIssuesGroup(report, ExtraReportGroup.ExternalDataMatchingIssues);
        
        ReportExtraAtvkEntries(report, ExtraReportGroup.ExternalDataMatchingIssues, atvkEntries, addressData.Municipalities, "municipality");
        ReportExtraWikidataItems(report, ExtraReportGroup.ExternalDataMatchingIssues, wikidataData.Municipalities, addressData.Municipalities, "municipality");
        ReportWikidataMatchIssues(report, ExtraReportGroup.ExternalDataMatchingIssues, wikidataMatchIssues);
        ReportVdbMatchIssues(report, ExtraReportGroup.ExternalDataMatchingIssues, vdbMatchIssues);
        ReportMissingWikidataItems(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Municipalities);
        ReportMissingVdbEntries(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Municipalities, vdbData.Municipalities);
        ReportUnmatchedOsmWikidataValues(report, ExtraReportGroup.ExternalDataMatchingIssues, addressData.Municipalities, municipalityCorrelation);
    }


    private enum ExtraReportGroup
    {
        MunicipalityBoundaries,
        InvalidMunicipalities,
        ExternalDataMatchingIssues,
        ProposedChanges
    }
}
