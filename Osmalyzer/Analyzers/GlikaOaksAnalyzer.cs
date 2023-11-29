using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class GlikaOaksAnalyzer : Analyzer
{
    public override string Name => "Glika Oaks";

    public override string Description => "This report checks that all Glika Ozoli oak trees are mapped.";

    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(GlikaOzoliAnalysisData) };


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmTrees = osmMasterData.Filter(
            new IsNode(),
            new HasValue("natural", "tree")
        );
            
        // Get Oak data

        List<GlikaOak> oaks = datas.OfType<GlikaOzoliAnalysisData>().First().Oaks;
            
        // Prepare data comparer/correlator

        Correlator<GlikaOak> dataComparer = new Correlator<GlikaOak>(
            osmTrees,
            oaks,
            new MatchDistanceParamater(15),
            new MatchFarDistanceParamater(75),
            new DataItemLabelsParamater("Glika oak", "Glika oaks"),
            new MatchCallbackParameter<GlikaOak>(DoesOsmTreeMatchOak)
        );
        
        [Pure]
        static MatchStrength DoesOsmTreeMatchOak(GlikaOak oak, OsmElement osmTree)
        {
            string? name = osmTree.GetValue("name");

            if (name == null)
                return MatchStrength.Unmatched;

            if (name.ToLower().Contains("glika ozols"))
                return MatchStrength.Strong;
            
            // todo: other stuff? monument denomination?
            
            return MatchStrength.Unmatched;
        }
            
        // Parse and report primary matching and location correlation

        dataComparer.Parse(
            report,
            new MatchedPairBatch(),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
            
        // todo: denomination
        // todo: species
        // todo: start_date
    }
}