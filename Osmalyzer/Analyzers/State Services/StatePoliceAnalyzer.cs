namespace Osmalyzer;

[UsedImplicitly]
public class StatePoliceAnalyzer : Analyzer
{
    public override string Name => "State police offices";

    public override string Description => "This report checks that all state police offices listed on government's website are found on the map.";

    public override AnalyzerGroup Group => AnalyzerGroup.StateServices;

    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(StatePoliceAnalysisData)
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

        // Load post office data
        List<StatePoliceData> listedPoliceOffices = datas.OfType<StatePoliceAnalysisData>().First().Offices;
        
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
        CorrelatorReport _ = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
    }
}