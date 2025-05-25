namespace Osmalyzer;

[UsedImplicitly]
public class GlikaOaksAnalyzer : Analyzer
{
    public override string Name => "Glika Oaks";

    public override string Description => "This report checks that all Glika Ozoli oak trees are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;

    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(GlikaOzoliAnalysisData) ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmTrees = osmMasterData.Filter(
            new IsNode(),
            new HasValue("natural", "tree")
        );
            
        // Get Oak data

        List<GlikaOak> oaks = datas.OfType<GlikaOzoliAnalysisData>().First().Oaks;
            
        // Prepare data comparer/correlator

        Correlator<GlikaOak> correlator = new Correlator<GlikaOak>(
            osmTrees,
            oaks,
            new MatchFarDistanceParamater(300), // some are poorly-placed
            new DataItemLabelsParamater("Glika oak", "Glika oaks"),
            new MatchCallbackParameter<GlikaOak>(DoesOsmTreeMatchOak),
            new LoneElementAllowanceParameter(DoesTreeAppearToBeGlika)
        );
        
        [Pure]
        static MatchStrength DoesOsmTreeMatchOak(GlikaOak oak, OsmElement osmTree)
        {
            return 
                DoesTreeAppearToBeGlika(osmTree) ?
                    MatchStrength.Strong :
                    MatchStrength.Unmatched;
            // todo: other stuff? monument denomination?
            
        }

        [Pure]
        static bool DoesTreeAppearToBeGlika(OsmElement osmTree)
        {
            string? name = osmTree.GetValue("name");

            return name != null && name.ToLower().Contains("glika ozols");
        }
            
        // Parse and report primary matching and location correlation

        correlator.Parse(
            report,
            new MatchedPairBatch(),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new MatchedLoneOsmBatch(true)
        );
            
        // todo: denomination
        // todo: species
        // todo: start_date
    }
}