namespace Osmalyzer;

[UsedImplicitly]
public class CourthouseAnalyzer : Analyzer
{
    public override string Name => "Courthouses";

    public override string Description => "This report checks that all official courthouses listed on government's website are found on the map.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(CourthouseAnalysisData) ];
        
    
    private readonly string[] _courthouseNameKeywords = [ 
        "tiesu nams", // e.g. "Administratīvās rajona tiesas Jelgavas tiesu nams"
        "rajona tiesa", // e.g. "Kurzemes rajona tiesa"
        "apgabaltiesa", // e.g. "Kurzemes apgabaltiesa"
        "augstākā tiesa" // i.e. "Latvijas Republikas Augstākā tiesa"
    ];
    

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
            if (SeemsLikeDifferentTypeOfCourthouse(element))
                return MatchStrength.Unmatched;

            if (FuzzyAddressMatcher.Matches(element, point.Courthouse.Address))
            {
                if (GoodNameMatch(element, point.Courthouse))
                    return MatchStrength.Strong;
                else
                    return MatchStrength.Good;
            }

            return MatchStrength.Regular;
        }

        [Pure]
        bool GoodNameMatch(OsmElement element, CourthouseData courthouse)
        {
            string? name = element.GetValue("name");
            
            if (name == null)
                return false;

            // Exact match
            if (name.Contains(courthouse.Name, StringComparison.InvariantCultureIgnoreCase) ||
                courthouse.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase))
                return true;
            
            // Key word match (specific, rather than generic like "tiesa"
            foreach (string keyword in _courthouseNameKeywords)
                if (name.Contains(keyword, StringComparison.InvariantCultureIgnoreCase) &&
                    courthouse.Name.Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                    return true;

            return false;
        }

        [Pure]
        bool SeemsLikeRecognizedCourthouse(OsmElement element)
        {
            if (SeemsLikeDifferentTypeOfCourthouse(element))
                return false;
            
            string? name = element.GetValue("name");

            if (name != null)
                foreach (string nameKeyword in _courthouseNameKeywords)
                    if (name.Contains(nameKeyword, StringComparison.InvariantCultureIgnoreCase))
                        return true;

            return false;
        }

        [Pure]
        bool SeemsLikeDifferentTypeOfCourthouse(OsmElement element)
        {
            string? name = element.GetValue("name");

            if (name == null)
                return false; // it might be, no name tho
            
            // amenity=courthouse deals with lots of stuff
            // Don't match against non-courthouses, like "bāriņtiesa", they tend to be in the same locations, but are not "pure" "tiesas"
            
            return name.ToLower().Contains("zemesgrāmat") || // e.g. "Ogres Rajona Tiesas Zemesgrāmatu nodaļa"
                   name.ToLower().Contains("bāriņties"); // e.g. "Daugavpils pilsētas Bāriņtiesa"
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport correlation = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new MatchedLoneOsmBatch(true)
        );

        // Offer updates to matched courthouse values (name, phones, email)
        
        List<MatchedCorrelation<LocatedCourthouse>> matchedPairs = correlation.Correlations
            .OfType<MatchedCorrelation<LocatedCourthouse>>()
            .ToList();

        if (matchedPairs.Count > 0)
        {
            List<TagComparison<LocatedCourthouse>> comparisons = [
                new TagComparison<LocatedCourthouse>(
                    "name",
                    lc => lc.Courthouse.Name
                ),
                new TagComparison<LocatedCourthouse>(
                    "email",
                    lc => lc.Courthouse.Email
                ),
                new TagComparison<LocatedCourthouse>(
                    "phone",
                    lc => string.Join(";", lc.Courthouse.Phones),
                    TagUtils.ValuesMatch
                ),
                new TagComparison<LocatedCourthouse>(
                    "opening_hours",
                    lc => lc.Courthouse.OpeningHours,
                    TagUtils.ValuesMatchOrderSensitive // prefer "sorted" days
                )
            ];

            TagSuggester<LocatedCourthouse> suggester = new TagSuggester<LocatedCourthouse>(
                matchedPairs,
                lc => lc.Courthouse.Name,
                "courthouse"
            );

            suggester.Suggest(
                report,
                comparisons
            );
        }

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
        
        // List all
        
        report.AddGroup(
            ExtraReportGroup.AllCourthouses,
            "All Courthouses"
        );

        foreach (CourthouseData courthouse in courthouseData.Courthouses)
        {
            report.AddEntry(
                ExtraReportGroup.AllCourthouses,
                new IssueReportEntry(
                    courthouse.ReportString()
                )
            );
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
        public string Name => Courthouse.Name;
        
        public string ReportString() => Courthouse.ReportString();
    }
    
    private enum ExtraReportGroup
    {
        UnlocatedCourthouses,
        AllCourthouses
    }
}