using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class CourthouseAnalyzer : Analyzer
{
    public override string Name => "Courthouses";

    public override string Description => "This report checks that all official courthouses listed on government's website are found on the map.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(CourthouseAnalysisData) };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
                
        OsmDataExtract osmCourthouses = osmMasterData.Filter(
            new HasValue("amenity", "courthouse")
        );

        // Load Courthouse data

        CourthouseAnalysisData courthouseData = datas.OfType<CourthouseAnalysisData>().First();

        List<CourthouseData> listedCourthouses = courthouseData.Courthouses.ToList();

        // Prepare data comparer/correlator

        Correlator<CourthouseData> dataComparer = new Correlator<CourthouseData>(
            osmCourthouses,
            listedCourthouses,
            new MatchDistanceParamater(100), // most data is like 50 meters away
            new MatchFarDistanceParamater(300),
            new MatchExtraDistanceParamater(MatchStrength.Strong, 700), // allow really far for exact matches
            new DataItemLabelsParamater("courthouse", "courthouses"),
            new OsmElementPreviewValue("name", false),
            new MatchCallbackParameter<CourthouseData>(GetMatchStrength),
            new LoneElementAllowanceCallbackParameter(SeemsLikeRecognizedCourthouse)
        );
        
        // todo: report closest potential (brand-untagged) courthouse when not matching anything?

        [Pure]
        MatchStrength GetMatchStrength(CourthouseData point, OsmElement element)
        {
            if (FuzzyAddressMatcher.Matches(element, point.Address))
                return MatchStrength.Strong;
                
            return MatchStrength.Good;
        }

        [Pure]
        bool SeemsLikeRecognizedCourthouse(OsmElement element)
        {
            string? name = element.GetValue("name");

            if (name != null)
            {
                if (!name.ToLower().Contains("zemesgrāmat") && // e.g. "Ogres Rajona Tiesas Zemesgrāmatu nodaļa"
                    !name.ToLower().Contains("bāriņties")) // e.g. "Daugavpils pilsētas Bāriņtiesa"
                {
                    if (name.ToLower().Contains("rajona tiesa") ||
                        name.ToLower().Contains("apgabaltiesa") ||
                        name.ToLower().Contains("augstākā tiesa"))
                        return true;
                }
            }

            return false;
        }

        // Parse and report primary matching and location correlation

        dataComparer.Parse(
            report,
            new MatchedPairBatch(),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new MatchedLoneOsmBatch(true)
        );
    }
}