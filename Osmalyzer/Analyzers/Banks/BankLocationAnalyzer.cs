namespace Osmalyzer;

[UsedImplicitly]
public abstract class BankLocationAnalyzer<TData> : Analyzer where TData : BankPointAnalysisData
{
    public override string Name => BankName + " Locations";

    public override string Description => "This report checks that all POIs from " + BankName + " contact list are mapped. " +
                                          "Note that the website list is not precise and the actual points can be dozens and even hundreds of meters away, such as in shopping malls.";

    public override AnalyzerGroup Group => AnalyzerGroup.Banks;

    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData), typeof(TData) ];


    protected abstract string BankName { get; }


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData OsmData = osmData.MasterData;

        OsmData allOsmPoints = OsmData.Filter(
            new HasAnyValue("amenity", "atm", "bank"),
            new CustomMatch(IsRelatedToBank)
        );
        
        [Pure]
        bool IsRelatedToBank(OsmElement osmElement)
        {
            string? osmName =
                osmElement.GetValue("operator") ??
                osmElement.GetValue("brand") ??
                osmElement.GetValue("name") ??
                null;

            return osmName != null && osmName.ToLower().Contains(BankName.ToLower());
        }

        OsmData osmAtms = allOsmPoints.Filter(
            new HasValue("amenity", "atm")
        );

        OsmData osmBranches = allOsmPoints.Filter(
            new HasValue("amenity", "bank")
        );

        // Get Bank data

        List<BankPoint> allPoints = datas.OfType<BankPointAnalysisData>().First().Points;
        
        List<BankAtmPoint> atmPoints = allPoints.OfType<BankAtmPoint>().ToList();

        List<BankBranchPoint> branchPoints = allPoints.OfType<BankBranchPoint>().ToList();

        // Correlate

        Correlate(osmAtms, atmPoints, "ATM", "ATMs");
        
        Correlate(osmBranches, branchPoints, "branch", "branches");
        

        void Correlate<TItem>(OsmData osmPoints, List<TItem> dataPoints, string labelSingular, string labelPlural) where TItem : BankPoint
        {
            // Prepare data comparer/correlator

            Correlator<TItem> correlator = new Correlator<TItem>(
                osmPoints,
                dataPoints,
                new FilterItemsToPolygonParamater(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), false),
                new MatchDistanceParamater(100),
                new MatchFarDistanceParamater(300), // some are stupidly far, like at the opposite end of a shopping center from the website's point
                new MatchExtraDistanceParamater(MatchStrength.Strong, 700), // allow really far for exact matches
                new DataItemLabelsParamater(BankName + " " + labelSingular, BankName + " " + labelPlural),
                new MatchCallbackParameter<TItem>(GetMatchStrength)
            );

            [Pure]
            MatchStrength GetMatchStrength(TItem point, OsmElement element)
            {
                if (point.Address != null &&
                    FuzzyAddressMatcher.Matches(element, point.Address))
                    return MatchStrength.Strong;
                
                return MatchStrength.Good;
            }

            // Parse and report primary matching and location correlation

            correlator.Parse(
                report,
                new MatchedPairBatch(),
                new UnmatchedItemBatch(),
                new MatchedFarPairBatch(),
                new UnmatchedOsmBatch()
            );
        }
    }
}