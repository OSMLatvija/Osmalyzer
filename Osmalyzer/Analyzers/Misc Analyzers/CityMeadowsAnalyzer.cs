namespace Osmalyzer;

[UsedImplicitly]
public class CityMeadowsAnalyzer : Analyzer
{
    public override string Name => "Riga City Meadows";

    public override string Description => "This report checks that all Riga city meadows (pilsētas pļavas) are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;

    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(CityMeadowsAnalysisData) ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData OsmData = osmData.MasterData;

        OsmData osmTrees = OsmData.Filter(
            new OrMatch(
                new HasValue("natural", "grassland"),
                new HasValue("landuse", "grass")
            ),
            new InsidePolygon(BoundaryHelper.GetRigaPolygon(OsmData), OsmPolygon.RelationInclusionCheck.FuzzyLoose)
        );
            
        // Get meadow data

        List<CityMeadow> meadows = datas.OfType<CityMeadowsAnalysisData>().First().Meadows;
            
        // Prepare data comparer/correlator

        Correlator<CityMeadow> correlator = new Correlator<CityMeadow>(
            osmTrees,
            meadows,
            new MatchDistanceParamater(50),
            new MatchFarDistanceParamater(100),
            new DataItemLabelsParamater("Meadow", "Meadows"),
            new MatchCallbackParameter<CityMeadow>(DoesOsmElementMatchMeadow)
        );
        
        [Pure]
        static MatchStrength DoesOsmElementMatchMeadow(CityMeadow meadow, OsmElement element)
        {
            string? name = element.GetValue("name");

            if (name == meadow.Name)
                return MatchStrength.Strong;
            
            string? altName = element.GetValue("alt_name");
            
            if (altName == meadow.Name)
                return MatchStrength.Strong;
            
            string? description = element.GetValue("description");
            
            if (description != null && description.Contains("pilsētas pļava", StringComparison.CurrentCultureIgnoreCase))
                return MatchStrength.Good;
            
            return MatchStrength.Unmatched;
        }

        // Parse and report primary matching and location correlation

        correlator.Parse(
            report,
            new MatchedPairBatch(),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
    }
}