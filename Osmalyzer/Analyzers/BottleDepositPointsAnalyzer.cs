using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class BottleDepositPointsAnalyzer : Analyzer
{
    public override string Name => "Bottle Deposit Points";

    public override string Description => "This report checks that all bottle deposit points are found on the map." + Environment.NewLine +
                                          "Note that deposit points website may have errors, mainly large offsets, but also missing or incorrect locations.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>()
    {
        typeof(OsmAnalysisData),
        typeof(DepositPointsAnalysisData)
    };


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmDepositPoints = osmMasterData.Filter(
            new HasAnyValue("amenity", "vending_machine", "recycling"),
            new CustomMatch(IsRelatedToDepositPoint)
        );

        [Pure]
        bool IsRelatedToDepositPoint(OsmElement osmElement)
        {
            string? osmName =
                osmElement.GetValue("brand") ??
                osmElement.GetValue("name") ??
                null;

            return osmName != null && osmName.ToLower().Contains("Depozīta punkts".ToLower());
        }

        // Load Deposit point data

        DepositPointsAnalysisData depositPointData = datas.OfType<DepositPointsAnalysisData>().First();

        List<DepositPoint> listedDepositPoints = depositPointData.DepositPoints.ToList();

        // Prepare data comparer/correlator

        Correlator<DepositPoint> dataComparer = new Correlator<DepositPoint>(
            osmDepositPoints,
            listedDepositPoints,
            new MatchDistanceParamater(100), // most data is like 50 meters away
            new MatchFarDistanceParamater(300), // some are really far from where the data says they ought to be
            new MatchExtraDistanceParamater(MatchStrength.Strong, 700), // allow really far for exact matches
            new DataItemLabelsParamater("deposit point", "deposit points"),
            new OsmElementPreviewValue("name", false),
            new LoneElementAllowanceCallbackParameter(_ => true),
            new MatchCallbackParameter<DepositPoint>(GetMatchStrength)
        );

        // todo: report closest potential (brand-untagged) shop when not matching anything?

        [Pure]
        MatchStrength GetMatchStrength(DepositPoint point, OsmElement element)
        {
            if (point.Address != null)
                if (FuzzyAddressMatcher.Matches(element, point.Address))
                    return MatchStrength.Strong;

            return MatchStrength.Good;
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