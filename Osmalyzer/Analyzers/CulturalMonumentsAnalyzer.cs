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
            new MatchDistanceParamater(30),
            new MatchFarDistanceParamater(300),
            new DataItemLabelsParamater("monument", "monuments"),
            new MatchCallbackParameter<CulturalMonument>(DoesOsmNodeMatchMonument),
            new OsmElementPreviewValue("name", false),
            new LoneElementAllowanceCallbackParameter(IsOsmMonumentAnExpectedHeritagePoi)
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

        [Pure]
        static bool IsOsmMonumentAnExpectedHeritagePoi(OsmElement osmElement)
        {
            string? osmRefStr = osmElement.GetValue("ref:LV:vkpai");

            if (osmRefStr != null)
                return true;

            string? herOper = osmElement.GetValue("heritage:operator");

            if (herOper != null)
            {
                herOper = herOper.ToLower();

                if (herOper.Contains("vkpai") ||
                    herOper.Contains("valsts kultūras pieminekļu aizsardzības inspekcija"))
                    return true;
            }

            return false;
        }
            
        // Parse and report primary matching and location correlation

        dataComparer.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new UnmatchedOsmBatch()
        );
    }
}