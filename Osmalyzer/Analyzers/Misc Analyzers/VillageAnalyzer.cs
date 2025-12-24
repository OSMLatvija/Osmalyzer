namespace Osmalyzer;

[UsedImplicitly]
public class VillageAnalyzer : Analyzer
{
    public override string Name => "Villages";

    public override string Description => "This report checks that all villages are mapped.";

    public override AnalyzerGroup Group => AnalyzerGroup.Miscellaneous;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(AddressGeodataAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmVillages = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("boundary", "administrative"),
            new HasValue("admin_level", "9"),
            new DoesntHaveKey("EHAK:code"), // some Estonian villages still leak over the border, but they seem to have import values we can use
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy) // lots around edges
        );
        
        // place=village

        // Get village data

        AddressGeodataAnalysisData adddressData = datas.OfType<AddressGeodataAnalysisData>().First();

        // Prepare data comparer/correlator

        Correlator<Village> correlator = new Correlator<Village>(
            osmVillages,
            adddressData.Villages.Where(v => v.Valid).ToList(),
            new MatchDistanceParamater(500), // todo: lower distance, but allow match inside relation
            new MatchFarDistanceParamater(2000),
            new MatchCallbackParameter<Village>(GetMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("village", "villages"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeAVillage)
        );

        [Pure]
        MatchStrength GetMatchStrength(Village village, OsmElement osmElement)
        {
            string? name = osmElement.GetValue("name");

            if (name == village.Name)
                return MatchStrength.Strong; // exact match on name

            if (DoesOsmElementLookLikeAVillage(osmElement))
                return MatchStrength.Good; // looks like a village, but not exact match
            
            return MatchStrength.Unmatched;
        }

        [Pure]
        bool DoesOsmElementLookLikeAVillage(OsmElement element)
        {
            string? place = element.GetValue("place");
            if (place == "village")
                return true; // explicitly tagged as village
            
            string? name = element.GetValue("name");
            if (name?.EndsWith("apkaime") == true)
                return false; // e.g. Riga suburb
            
            return true;
        }

        // Parse and report primary matching and location correlation

        correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
        
        // Validate additional issues

        // todo:
        
        // List invalid villages that are still in data
        
        // Create a group and dump all invalid village entries from geodata for awareness/tracking
        report.AddGroup(
            ExtraReportGroup.InvalidVillages,
            "Invalid Villages",
            "Villages marked invalid in address geodata (not approved or not existing).",
            "There are no invalid villages in the geodata."
        );

        List<Village> invalidVillages = adddressData.Villages.Where(v => !v.Valid).ToList();

        foreach (Village village in invalidVillages)
        {
            report.AddEntry(
                ExtraReportGroup.InvalidVillages,
                new IssueReportEntry(
                    village.ReportString()
                )
            );
        }
    }


    private enum ExtraReportGroup
    {
        InvalidVillages
    }
}