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

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;

    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(CulturalMonumentsMapAnalysisData) };


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmHeritages = osmMasterData.Filter(
            new OrMatch(
                new HasKey("name"),
                new HasKey("heritage"),
                new HasKey("heritage:operator"),
                new HasKey("ref:LV:vkpai")
            )
        );
            
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
            new LoneElementAllowanceCallbackParameter(IsOsmElementHeritagePoiByItself)
        );
        
        [Pure]
        static MatchStrength DoesOsmNodeMatchMonument(CulturalMonument monument, OsmElement osmElement)
        {
            // name
            
            if (osmElement.GetValue("name")?.ToLower() == monument.Name)
                return MatchStrength.Strong;
            
            // ref:LV:vkpai
            
            string? osmRefStr = osmElement.GetValue("ref:LV:vkpai");

            if (osmRefStr != null)
            {
                if (int.TryParse(osmRefStr, out int osmRef))
                    if (osmRef == monument.ReferenceID)
                        return MatchStrength.Strong;
                
                return MatchStrength.Good;
            }
            
            // heritage
            
            string? heritageStr = osmElement.GetValue("heritage");

            if (heritageStr != null)
            {
                if (int.TryParse(osmRefStr, out int osmRef))
                    if (osmRef == 2)
                        return MatchStrength.Good;
                
                return MatchStrength.Regular;
            }

            // heritage:operator
            
            string? herOperStr = osmElement.GetValue("heritage:operator");

            if (herOperStr != null)
            {
                herOperStr = herOperStr.ToLower();

                if (herOperStr.Contains("vkpai") ||
                    herOperStr.Contains("valsts kultūras pieminekļu aizsardzības inspekcija"))
                    return MatchStrength.Good;
                
                return MatchStrength.Regular;
            }
            
            return MatchStrength.Unmatched;
        }

        [Pure]
        static bool IsOsmElementHeritagePoiByItself(OsmElement osmElement)
        {
            // ref:LV:vkpai
            
            string? osmRefStr = osmElement.GetValue("ref:LV:vkpai");

            if (osmRefStr != null)
                return true;

            // heritage:operator
            
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
            new MatchedFarPairBatch()
        );
    }
}