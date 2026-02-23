namespace Osmalyzer;

[UsedImplicitly]
public class StatePoliceAnalyzer : Analyzer
{
    public override string Name => "State police offices";

    public override string Description => "This report checks that all state police offices listed on government's website are found on the map " +
                                          "and that they have the correct tags.";

    public override AnalyzerGroup Group => AnalyzerGroup.StateServices;

    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(StatePoliceListAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData OsmData = osmData.MasterData;
                
        OsmData osmPoliceOffices = OsmData.Filter(
            new HasAnyValue("amenity", "police"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.FuzzyLoose) // a couple OOB hits
        );

        // Load police office data

        List<StatePoliceData> listedPoliceOffices = datas.OfType<StatePoliceListAnalysisData>().First().Offices;

        // Prepare data comparer/correlator

        Correlator<StatePoliceData> correlator = new Correlator<StatePoliceData>(
            osmPoliceOffices,
            listedPoliceOffices,
            new MatchDistanceParamater(100),
            new MatchFarDistanceParamater(200),
            new MatchExtraDistanceParamater(MatchStrength.Strong, 500),
            new DataItemLabelsParamater("State police office", "State police offices"),
            new OsmElementPreviewValue("name", true)
        );
        
        // Parse and report primary matching and location correlation

        CorrelatorReport correlatorReport = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        // Validate
        
        Validator<StatePoliceData> validator = new Validator<StatePoliceData>(
            correlatorReport,
            "Tagging issues"
        );

        List<SuggestedAction> suggestedChanges = validator.Validate(
            report,
            false, false,
            new ValidateElementValueMatchesDataItemValue<StatePoliceData>("name", h => h.Name), // todo: can we shorten these meaningfully as common/primary name?
            new ValidateElementValueMatchesDataItemValue<StatePoliceData>("official_name", h => h.Name),
            new ValidateElementValueMatchesDataItemValue<StatePoliceData>("short_name", h => h.AbbreviatedName),
            new ValidateElementHasValue("operator", "Valsts policija"),
            new ValidateElementHasValue("operator:wikidata", "Q3741089"),
            new ValidateElementHasValue("operator:type", "government"),
            new ValidateElementValueMatchesDataItemValue<StatePoliceData>("website", h => h.Website),
            new ValidateElementValueMatchesDataItemValue<StatePoliceData>("email", h => h.Email),
            new ValidateElementValueMatchesDataItemValue<StatePoliceData>("phone", h => h.Phone),
            new ValidateElementValueMatchesDataItemValue<StatePoliceData>("opening_hours", h => h.OpeningHours)
            // todo: new ValidateElementHasValue("police:LV", "state") -- as opposed to municipal
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(OsmData, suggestedChanges, this, "changes");
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif
        
        // List all
        
        report.AddGroup(
            ExtraReportGroup.AllStations,
            "All Stations"
        );

        foreach (StatePoliceData policeOffice in listedPoliceOffices)
        {
            report.AddEntry(
                ExtraReportGroup.AllStations,
                new IssueReportEntry(
                    policeOffice.ReportString()
                )
            );
        }
    }

    
    private enum ExtraReportGroup
    {
        AllStations,
        ProposedChanges
    }
}