namespace Osmalyzer;

[UsedImplicitly]
[DisabledAnalyzer("LP data doesn't provide mail boxes")]
public class LatviaPostMailBoxAnalyzer : Analyzer
{
    protected string Operator { get; } = "Latvijas Pasts";

    public override string Name => Operator + " Mail boxes";

    public override string Description => "This report checks that all " + Operator + " mail boxes listed on company's website are found on the map." + Environment.NewLine +
                                          "Note that Latvijas pasts' website can and does have errors: mainly incorrect positions, but sometimes missing or phantom items too.";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;

    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(LatviaPostAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData OsmData = osmData.MasterData;
                
        OsmData osmPostBoxes = OsmData.Filter(
            new HasAnyValue("amenity", "post_box")
        );

        // Load Parcel locker data
        List<LatviaPostItem> listedItems  = datas.OfType<LatviaPostAnalysisData>().First().LatviaPostItems;
        
        List<LatviaPostItem> listedBoxes  = listedItems.Where(i => i.ItemType == LatviaPostItemType.PostBox).ToList();

        // Prepare data comparer/correlator

        Correlator<LatviaPostItem> correlator = new Correlator<LatviaPostItem>(
            osmPostBoxes,
            listedBoxes,
            new MatchDistanceParamater(100),
            new MatchFarDistanceParamater(200),
            new MatchExtraDistanceParamater(MatchStrength.Strong, 500),
            new DataItemLabelsParamater(Operator + " mail box", Operator + " mail boxes"),
            new OsmElementPreviewValue("name", false),
            new MatchCallbackParameter<LatviaPostItem>(GetMatchStrength)
        );
        
        [Pure]
        MatchStrength GetMatchStrength(LatviaPostItem point, OsmElement element)
        {
            if (point.Address != null)
                if (FuzzyAddressMatcher.Matches(element, point.Address))
                    return MatchStrength.Strong;
                
            return MatchStrength.Good;
        }

        // Parse and report primary matching and location correlation

        correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new UnmatchedOsmBatch()
        );
    }
}