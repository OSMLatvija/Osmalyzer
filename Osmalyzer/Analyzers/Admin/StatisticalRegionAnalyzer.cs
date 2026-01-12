namespace Osmalyzer;

[UsedImplicitly]
public class StatisticalRegionAnalyzer : Analyzer
{
    public override string Name => "Statistical Regions";

    public override string Description => "This report checks statistical regions (statistiskie reģioni).";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ 
        typeof(LatviaOsmAnalysisData), 
        typeof(AtvkAnalysisData),
        typeof(CspPopulationAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmData OsmData = osmData.MasterData;

        OsmData osmHistoricalLands = OsmData.Filter(
            new IsRelation(),
            new HasValue("boundary", "statistical"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside)
        );

        // Get all data sources

        AtvkAnalysisData atvkData = datas.OfType<AtvkAnalysisData>().First();
        
        CspPopulationAnalysisData cspData = datas.OfType<CspPopulationAnalysisData>().First();

        List<AtvkEntry> atvkRegions = atvkData.Entries.Where(e => e.Designation == AtvkDesignation.Region).ToList();
        if (atvkRegions.Count == 0) throw new Exception("No ATVK statistical region entries found.");
        
        // Assign CSP population data
        
        cspData.AssignToDataItems(
            atvkRegions,
            CspAreaType.Region,
            _ => null, // not doing lookups by name
            i => i.Code,
            _ => null // none should need it
        );
        
        // Prepare data comparer/correlator

        Correlator<AtvkEntry> correlator = new Correlator<AtvkEntry>(
            osmHistoricalLands,
            atvkRegions,
            new MatchAnywhereParamater(),
            // TODO: where can I get coords for these? neither csp nor atvk have them
            // new MatchDistanceParamater(25000),
            // new MatchFarDistanceParamater(75000),
            new MatchCallbackParameter<AtvkEntry>(GetRegionMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("statistical region", "statistical regions"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeHistoricalLand)
        );

        [Pure]
        MatchStrength GetRegionMatchStrength(AtvkEntry entry, OsmElement osmElement)
        {
            string? refNum = osmElement.GetValue("ref");
            if (refNum == entry.Code)
                return MatchStrength.Strong; // exact match on code

            string? name = osmElement.GetValue("name");
            if (name != null && entry.Name.StartsWith(name)) // e.g. "Latgale" vs "Latgales statistiskais reģions"
                return MatchStrength.Strong; // great match on name

            // todo: alt name

            return MatchStrength.Unmatched;
        }

        [Pure]
        bool DoesOsmElementLookLikeHistoricalLand(OsmElement element)
        {
            return true;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport correlatorReport = correlator.Parse(
            report, 
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true)
            //new UnmatchedItemBatch(),
            //new MatchedFarPairBatch()
        );

        // Validate municipality syntax
        
        Validator<AtvkEntry> municipalityValidator = new Validator<AtvkEntry>(
            correlatorReport,
            "Region syntax issues"
        );

        List<SuggestedAction> suggestedChanges = municipalityValidator.Validate(
            report,
            false, false,
            new ValidateElementValueMatchesDataItemValue<AtvkEntry>(e => e.UserData == null, "population", c => c.CspPopulationEntry?.Population.ToString()),
            new ValidateElementValueMatchesDataItemValue<AtvkEntry>(e => e.UserData == null, "source:population", c => c.CspPopulationEntry?.Source)
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(OsmData, suggestedChanges, this);
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif
    }


    private enum ExtraReportGroup
    {
        ProposedChanges
    }
}