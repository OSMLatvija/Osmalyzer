using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class BottleDepositPointsAnalyzer : Analyzer
{
    public override string Name => "Depozīta punkts";

    public override string Description => "This report checks that all bottle deposit points are found on the map." + Environment.NewLine +
                                          "Note that deposit points website has errors: large offsets, missing locations " +
                                          "and incorrect number of taromats in place.";

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
            new HasAnyValue("amenity", "recycling"),
            new CustomMatch(IsRelatedToDepositPoint)
        );

        OsmDataExtract osmDepositAutomats = osmMasterData.Filter(
            new HasAnyValue("amenity", "vending_machine"),
            new CustomMatch(IsRelatedToDepositPoint)
        );

        [Pure]
        bool IsRelatedToDepositPoint(OsmElement osmElement)
        {
            string? osmName =
                osmElement.GetValue("brand") ??
                osmElement.GetValue("name") ??
                null;

            return osmName != null 
                && (osmName.ToLower().Contains("Depozīta".ToLower())
                    || osmName.ToLower().Contains("Deposit".ToLower()));
        }

        // Load Deposit point data

        DepositPointsAnalysisData depositPointData = datas.OfType<DepositPointsAnalysisData>().First();

        List<DepositPoint> listedDepositPoints = depositPointData.DepositPoints
            .Where(_ => _.Mode != DepositPoint.DepositPointMode.Manual).ToList(); // Only automated for now
        List<DepositPoint.DepositAutomat> listedDepositAutomats = depositPointData.DepositAutomats;

        Correlate(osmDepositPoints, listedDepositPoints, "deposit point", "deposit points");
        Correlate(osmDepositAutomats, listedDepositAutomats, "deposit automat", "deposit automats");

        void Correlate<TItem>(OsmDataExtract osmPoints, List<TItem> dataPoints, string labelSingular, string labelPlural) where TItem : DepositPoint
        {
            // Prepare data comparer/correlator

            Correlator<TItem> dataComparer = new Correlator<TItem>(
                osmPoints,
                dataPoints,
                new MatchDistanceParamater(50), // most data is like 50 meters away
                new MatchFarDistanceParamater(150), // some are really far from where the data says they ought to be
                new MatchExtraDistanceParamater(MatchStrength.Strong, 500), // allow really far for exact matches
                new DataItemLabelsParamater(labelSingular, labelPlural),
                new OsmElementPreviewValue("name", false),
                new LoneElementAllowanceCallbackParameter(_ => true),
                new MatchCallbackParameter<DepositPoint>(GetMatchStrength)
            );

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
}