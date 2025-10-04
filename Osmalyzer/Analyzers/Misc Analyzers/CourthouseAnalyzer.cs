using System.Diagnostics;

namespace Osmalyzer;

[UsedImplicitly]
public class CourthouseAnalyzer : Analyzer
{
    public override string Name => "Courthouses";

    public override string Description => "This report checks that all official courthouses listed on government's website are found on the map.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(CourthouseAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
                
        OsmDataExtract osmCourthouses = osmMasterData.Filter(
            new HasValue("amenity", "courthouse")
        );

        // Load Courthouse data

        CourthouseAnalysisData courthouseData = datas.OfType<CourthouseAnalysisData>().First();

        List<CourthouseData> listedCourthouses = courthouseData.Courthouses.ToList();

        // Geolocate courthouses from their addresses
        
        List<LocatedCourthouse> locatedCourthouses = [ ];
        List<CourthouseData> unlocatedCourthouses = [ ];

        foreach (CourthouseData ch in listedCourthouses)
        {
            LocatedCourthouse? located = TryLocateCourthouse(ch, osmMasterData);
            if (located != null)
                locatedCourthouses.Add(located);
            else
                unlocatedCourthouses.Add(ch);
        }

        // Prepare data comparer/correlator

        Correlator<LocatedCourthouse> correlator = new Correlator<LocatedCourthouse>(
            osmCourthouses,
            locatedCourthouses,
            new MatchDistanceParamater(100), // most data is like 50 meters away
            new MatchFarDistanceParamater(300),
            new MatchExtraDistanceParamater(MatchStrength.Strong, 700), // allow really far for exact matches
            new DataItemLabelsParamater("courthouse", "courthouses"),
            new OsmElementPreviewValue("name", false),
            new MatchCallbackParameter<LocatedCourthouse>(GetMatchStrength),
            new LoneElementAllowanceParameter(SeemsLikeRecognizedCourthouse)
        );
        
        // todo: report closest potential (brand-untagged) courthouse when not matching anything?

        [Pure]
        MatchStrength GetMatchStrength(LocatedCourthouse point, OsmElement element)
        {
            if (FuzzyAddressMatcher.Matches(element, point.Courthouse.Address))
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

        correlator.Parse(
            report,
            new MatchedPairBatch(),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new MatchedLoneOsmBatch(true)
        );

        // Report any courthouses we couldn't geolocate by address
        if (unlocatedCourthouses.Count > 0)
        {
            report.AddGroup(
                ExtraReportGroup.UnlocatedCourthouses,
                "Non-geolocated Courthouses",
                "These listed courthouses could not be geolocated to an OSM address. " +
                "Possibly, the data values are incorrect, differently-formatted or otherwise fail to match automatically."
            );

            foreach (CourthouseData unlocated in unlocatedCourthouses)
            {
                report.AddEntry(
                    ExtraReportGroup.UnlocatedCourthouses,
                    new IssueReportEntry(
                        "Courthouse `" + unlocated.Name + "` could not be geolocated for `" + unlocated.Address + "`"
                    )
                );
            }
        }
    }


    [Pure]
    private static LocatedCourthouse? TryLocateCourthouse(CourthouseData ch, OsmMasterData osmData)
    {
        OsmCoord? coord = FuzzyAddressFinder.Find(
            osmData, 
            ch.Address,
            // all are of form "Aiviekstes iela 6, Rīga, LV-1019"
            new FuzzyAddressStreetLineHint(0), 
            new FuzzyAddressCityHint(1), 
            new FuzzyAddressPostcodeHint(2)
        );

        if (coord == null)
            return null;

        return new LocatedCourthouse(ch, coord.Value);
    }

    private record LocatedCourthouse(CourthouseData Courthouse, OsmCoord Coord) : IDataItem
    {
        public string ReportString() => Courthouse.ReportString();
    }
    
    private enum ExtraReportGroup
    {
        UnlocatedCourthouses
    }
}