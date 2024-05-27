using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class BottleDepositPointsAnalyzer : Analyzer
{
    public override string Name => "Depozīta punkti";

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

        OsmDataExtract osmAutomatedDepositLoactions = osmMasterData.Filter(
            new HasAnyValue("amenity", "recycling"),
            new CustomMatch(IsRelatedToDepositPoint)
        );

        OsmDataExtract osmManualDepositLoactions = osmMasterData.Filter(
            new HasKey("shop"),
            new HasValue("recycling:cans","yes"),
            new HasValue("recycling:plastic_bottles","yes"),
            new HasValue("recycling:glass_bottles","yes")
        );

        OsmDataExtract osmDepositAutomats = osmMasterData.Filter(
            new HasValue("amenity", "vending_machine"),
            new HasValue("vending", "bottle_return")
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

        // General location correlation
        
        List<AutomatedDepositLocation> listedDepositKiosks = depositPointData.DepositKiosks;
        List<ManualDepositLocation> listedManualDepositLocations = depositPointData.ManualDepositLocations;
        List<DepositAutomat> listedDepositAutomats = depositPointData.DepositAutomats;

        Correlate(osmAutomatedDepositLoactions, listedDepositKiosks, "deposit kiosk", "deposit kiosks");
        Correlate(osmDepositAutomats, listedDepositAutomats, "deposit automat", "deposit automats");
        Correlate(osmManualDepositLoactions, listedManualDepositLocations, "manual deposit location", "manual deposit locations");
        
        void Correlate<TItem>(OsmDataExtract osmPoints, List<TItem> dataPoints, string labelSingular, string labelPlural) where TItem : DepositPoint
        {
            // Prepare data comparer/correlator

            Correlator<TItem> dataComparer = new Correlator<TItem>(
                osmPoints,
                dataPoints,
                new MatchDistanceParamater(75), // often data points to shop instead of kiosk
                new MatchFarDistanceParamater(150), // some are really far from where the data says they ought to be
                new MatchExtraDistanceParamater(MatchStrength.Strong, 500), // allow really far for exact matches
                new DataItemLabelsParamater(labelSingular, labelPlural),
                new OsmElementPreviewValue("name", false),
                new LoneElementAllowanceCallbackParameter(_ => true),
                new MatchCallbackParameter<DepositPoint>(GetMatchStrength)
            );

            // todo: compare shop names too or maybe even extract it from Correlate and make strength function per item type
            [Pure]
            MatchStrength GetMatchStrength(DepositPoint point, OsmElement element)
            {
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
        
        // Additional stats
        
        report.AddGroup(ExtraReportGroup.Stats, "Stats");

        ReportStats(depositPointData.DepositKiosks, "Kiosks");
        ReportStats(depositPointData.DepositAutomats, "Vending machines");
        ReportStats(depositPointData.ManualDepositLocations, "Manual returns");

        void ReportStats<TItem>(List<TItem> points, string label) where TItem : DepositPoint
        {
            Dictionary<string, int> shopCounts =
                points
                    .GroupBy(p => p.ShopName)
                    .OrderByDescending(g => g.Count())
                    .ToDictionary(g => g.Key, g => g.Count());
            
            report.AddEntry(
                ExtraReportGroup.Stats,
                new GenericReportEntry(
                    label + " are found in/near the following shops: " + string.Join(", ", shopCounts.Select(kvp => "`" + kvp.Key + "`" + " (x " + kvp.Value + ")"))
                )
            );
        }
    }
    
    
    private enum ExtraReportGroup
    {
        Stats
    }
}