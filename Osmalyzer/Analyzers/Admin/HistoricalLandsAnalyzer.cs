namespace Osmalyzer;

[UsedImplicitly]
public class HistoricalLandsAnalyzer : Analyzer
{
    public override string Name => "Historical Lands";

    public override string Description => "This report checks historical lands (vēsturiskās zemes).";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ 
        typeof(LatviaOsmAnalysisData), 
        typeof(HistoricalLandsAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmData OsmData = osmData.MasterData;

        OsmData osmHistoricalLands = OsmData.Filter(
            new IsRelation(),
            new HasValue("boundary", "traditional"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside)
        );

        // Get all data sources

        HistoricalLandsAnalysisData historicalLandsData = datas.OfType<HistoricalLandsAnalysisData>().First();

        // Prepare data comparer/correlator

        Correlator<HistoricalLand> historicalLandCorrelator = new Correlator<HistoricalLand>(
            osmHistoricalLands,
            historicalLandsData.HistoricalLands,
            new MatchDistanceParamater(25000),
            new MatchFarDistanceParamater(75000),
            new MatchCallbackParameter<HistoricalLand>(GetHistoricalLandMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("historical land", "historical lands"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeHistoricalLand)
        );

        [Pure]
        MatchStrength GetHistoricalLandMatchStrength(HistoricalLand historicalLand, OsmElement osmElement)
        {
            string? name = osmElement.GetValue("name");

            if (name == historicalLand.Name)
                return MatchStrength.Strong; // exact match on name
            
            return MatchStrength.Unmatched;
        }

        [Pure]
        bool DoesOsmElementLookLikeHistoricalLand(OsmElement element)
        {
            string? boundary = element.GetValue("boundary");
            if (boundary != "traditional")
                return false; // must be traditional boundary
            
            string? traditional = element.GetValue("traditional");
            if (traditional == "international")
                return false; // outside Latvia, i.e. Baltics
            
            string? name = element.GetValue("name");
            if (name?.Contains("Baltijas valstis") == true)
                return false; // specifically Baltics
            
            return true;
        }

        // Parse and report primary matching and location correlation

        historicalLandCorrelator.Parse(
            report, 
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        // todo: validate tagging
    }
}
