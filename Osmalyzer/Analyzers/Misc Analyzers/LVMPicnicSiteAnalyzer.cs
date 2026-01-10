namespace Osmalyzer;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public class LVMPicnicSiteAnalyzer : Analyzer
{
    public override string Name => "LVM Picnic Sites";

    public override string Description => "";

    public override AnalyzerGroup Group => AnalyzerGroup.POIs;

    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(LVMPicnicSiteAnalysisData)
    ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData OsmData = osmData.MasterData;

        OsmData osmPicnicSites = OsmData.Filter(
            new HasAnyValue("tourism", "picnic_site")
        );

        // Load picnic site data

        LVMPicnicSiteAnalysisData picnicSiteData = datas.OfType<LVMPicnicSiteAnalysisData>().First();

        List<LVMPicnicSiteData> listedPicnicSites = picnicSiteData.PicnicSites.ToList();

        // Prepare data comparer/correlator

        Correlator<LVMPicnicSiteData> correlator = new Correlator<LVMPicnicSiteData>(
            osmPicnicSites,
            listedPicnicSites,
            new MatchDistanceParamater(100),
            new MatchFarDistanceParamater(300), // some are really far from where the data says they ought to be
            new OsmElementPreviewValue("name", false)
        );

        // Parse and report primary matching and location correlation

        correlator.Parse(
            report,
            new MatchedPairBatch(),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );
    }
}