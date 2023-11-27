using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class CulturalMonumentsAnalyzer : Analyzer
{
    public override string Name => "Cultural Monuments";

    public override string Description => "This report checks that all cultural monument POIs are mapped.";

    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(CulturalMonumentsMapAnalysisData) };


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmHeritages = osmMasterData.Filter(
            new HasKey("heritage")
        );
        
        // heritage=2
        // heritage:operator=vkpai
        // ref:LV:vkpai=*
            
        // Get monument data

        List<CulturalMonument> monuments = datas.OfType<CulturalMonumentsMapAnalysisData>().First().Monuments;
            
        // Prepare data comparer/correlator

        Correlator<CulturalMonument> dataComparer = new Correlator<CulturalMonument>(
            osmHeritages,
            monuments,
            new MatchDistanceParamater(15),
            new MatchFarDistanceParamater(75),
            new DataItemLabelsParamater("monument", "monuments"),
            new MatchCallbackParameter<CulturalMonument>(DoesOsmNodeMatchMonument),
            new OsmElementPreviewValue("name", false)
        );
        
        [Pure]
        static bool DoesOsmNodeMatchMonument(CulturalMonument monument, OsmElement osmElement)
        {
            string? osmRefStr = osmElement.GetValue("ref:LV:vkpai");

            if (osmRefStr != null)
                if (int.TryParse(osmRefStr, out int osmRef))
                    if (osmRef == monument.ReferenceID)
                        return true;

            return osmElement.GetValue("name")?.ToLower() == monument.Name;
        }
            
        // Parse and report primary matching and location correlation

        dataComparer.Parse(
            report,
            new MatchedPairBatch(),
            //new MatchedLoneOsmBatch(true), -- todo: if we can assume it's marked as such
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new UnmatchedOsmBatch()
        );
    }
}