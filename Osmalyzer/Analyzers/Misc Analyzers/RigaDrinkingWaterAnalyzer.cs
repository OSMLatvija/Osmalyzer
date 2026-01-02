namespace Osmalyzer;

[UsedImplicitly]
public class RigaDrinkingWaterAnalyzer : Analyzer
{
    public override string Name => "Riga Drinking Water";

    public override string Description => "This report checks that drinking water taps for Riga are mapped and their tagging is correct. They are all expected to be free-standing drinkable water taps (brīvkrāni) operated by Rīgas ūdens.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(RigaDrinkingWaterAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmTaps = osmMasterData.Filter(
            new IsNode(),
            new HasValue("amenity", "drinking_water"),
            new DoesntHaveValue("indoor", "yes"), // none of Riga taps are indoors, so ignore these as they can be in malls, hospitals, etc.
            new InsidePolygon(BoundaryHelper.GetRigaPolygon(osmMasterData), OsmPolygon.RelationInclusionCheck.FuzzyLoose)
        );

        // Get Riga taps

        RigaDrinkingWaterAnalysisData drinkingWaterData = datas.OfType<RigaDrinkingWaterAnalysisData>().First();

        List<DrinkingWater> rigaTapsAll = drinkingWaterData.DrinkingWaters;
            
        List<DrinkingWater> rigaTapsStatic = rigaTapsAll.Where(t => t.Type == DrinkingWater.InstallationType.Static).ToList();
        // We don't care about their mobile ones since we wouldn't map them on OSM (although with this report we could keep track)
            
        // Prepare data comparer/correlator

        Correlator<DrinkingWater> correlator = new Correlator<DrinkingWater>(
            osmTaps,
            rigaTapsStatic,
            new DataItemLabelsParamater("Riga tap", "Riga taps"),
            new LoneElementAllowanceParameter(IsUnmatchedOsmElementAllowed),
            new OsmElementPreviewValue( // add a label for (non-)seasonal
                "seasonal", 
                false, 
                new OsmElementPreviewValue.PreviewLabel("yes", "seasonal tap"),
                new OsmElementPreviewValue.PreviewLabel("yes", "non-seasonal tap")
            )
        );
        // Note that we don't have any condition to mismatch taps, we only expect riga taps in riga

        bool IsUnmatchedOsmElementAllowed(OsmElement element)
        {
            // Riga tap list deletes their list during winter (rather than somehow tagging it)
            // Since OSM does not delete elements, but rather marks them as seasonal, we can keep assume those are correct
            return element.GetValue("seasonal") == "yes";
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport correlatorReport = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(false),
            new UnmatchedItemBatch(),
            new UnmatchedOsmBatch(),
            new MatchedFarPairBatch()
        );

        // Validate additional issues

        Validator<DrinkingWater> validator = new Validator<DrinkingWater>(
            correlatorReport
        );

        List<SuggestedAction> suggestedChanges = validator.Validate(
            report,
            true, // all elements we checked against are "real", so should follow the rules
            new ValidateElementHasValue("operator", "Rīgas ūdens"),
            new ValidateElementHasValue("man_made", "water_tap"),
            new ValidateElementHasValue("drinking_water", "yes"),
            new ValidateElementHasValue("seasonal", "yes", "no"),
            new ValidateElementFixme()
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this);
#endif
    }
}