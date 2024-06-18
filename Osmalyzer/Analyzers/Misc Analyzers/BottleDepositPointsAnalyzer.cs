using System;
using System.Collections.Generic;
using System.Linq;

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

        OsmDataExtract osmKioskDepositLocations = osmMasterData.Filter(
            new HasAnyValue("amenity", "recycling"),
            new CustomMatch(IsRelatedToDepositPoint)
        );

        OsmDataExtract osmManualDepositLocations = osmMasterData.Filter(
            new HasKey("shop"),
            new HasValue("recycling:cans","yes"),
            new HasValue("recycling:plastic_bottles","yes"),
            new HasValue("recycling:glass_bottles","yes")
        );

        OsmDataExtract osmVendingMachines = osmMasterData.Filter(
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
        
        List<KioskDepositPoint> listedDepositKiosks = depositPointData.Kiosks;
        List<ManualDepositPoint> listedManualDepositLocations = depositPointData.ManualLocations;
        List<VendingMachineDepositPoint> listedDepositVendingMachines = depositPointData.VendingMachines;

        CorrelatorReport kioskReport = Correlate(osmKioskDepositLocations, listedDepositKiosks, "kiosk", "kiosks");
        CorrelatorReport vendingMachineReport = Correlate(osmVendingMachines, listedDepositVendingMachines, "vending machine", "vending machines");
        CorrelatorReport manualLocationReport = Correlate(osmManualDepositLocations, listedManualDepositLocations, "manual location", "manual locations");

        CorrelatorReport Correlate<TItem>(OsmDataExtract osmPoints, List<TItem> dataPoints, string labelSingular, string labelPlural) where TItem : DepositPoint
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

            return dataComparer.Parse(
                report,
                new MatchedPairBatch(),
                new UnmatchedItemBatch(),
                new MatchedFarPairBatch(),
                new UnmatchedOsmBatch()
            );
        }
        
        // Tagging verification

        // CorrelatorReport comboReport = new CorrelatorReport(
        //     kioskReport,
        //     vendingMachineReport,
        //     manualLocationReport
        // );

        Validator<KioskDepositPoint> kisokValidator = new Validator<KioskDepositPoint>(
            kioskReport,
            "Other kiosk issues"
        );

        kisokValidator.Validate(
            report,
            new ValidateElementHasValue("name", "Depozīta punkts"),
            new ValidateElementHasValue("brand", "Depozīta punkts"),
            new ValidateElementHasValue("brand:wikidata", "Q110979381"),
            new ValidateElementHasValue("building", "kiosk"), 
            new ValidateElementHasValue("recycling:cans", "yes"),
            new ValidateElementHasValue("recycling:glass_bottles", "yes"),
            new ValidateElementHasValue("recycling:plastic_bottles", "yes"),
            new ValidateElementDoesntHaveTag("recycling_type"),
            // todo: operator needed ?
            new ValidateElementFixme()
        );

        Validator<VendingMachineDepositPoint> vendingValidator = new Validator<VendingMachineDepositPoint>(
            vendingMachineReport,
            "Other vending machine issues"
        );

        vendingValidator.Validate(
            report,
            new ValidateElementHasValue("name", "Depozīta punkts"),
            new ValidateElementHasValue("brand", "Depozīta punkts"),
            new ValidateElementHasValue("brand:wikidata", "Q110979381"),
            new ValidateElementHasValue("recycling:cans", "yes"),
            new ValidateElementHasValue("recycling:glass_bottles", "yes", "no"),
            new ValidateElementHasValue("recycling:plastic_bottles", "yes"),
            new ValidateElementDoesntHaveTag("building"),
            // todo: operator needed ?
            new ValidateElementFixme()
        );

        // Additional stats
        
        report.AddGroup(ExtraReportGroup.Stats, "Stats");

        ReportStats(depositPointData.Kiosks, "Kiosks");
        ReportStats(depositPointData.VendingMachines, "Vending machines");
        ReportStats(depositPointData.ManualLocations, "Manual returns");

        void ReportStats<TItem>(List<TItem> points, string label) where TItem : DepositPoint
        {
            // Shop names
            
            List<ShopCounter> counters = new List<ShopCounter>();
            int unspecified = 0;
            
            foreach (TItem point in points)
            {
                string? shopName = point.ShopName;

                if (shopName == null)
                {
                    unspecified++;
                    continue;
                }

                ShopCounter? counter = counters.FirstOrDefault(c => c.Names.Any(n => string.Equals(n, shopName, StringComparison.CurrentCultureIgnoreCase)));
                // There are a bunch of other stupid inconsistencies like "VIADA DUS" and DUS VIADA" and "VIADA", but not hard-coding every case

                if (counter == null)
                    counters.Add(new ShopCounter(shopName));
                else
                    counter.Add(shopName);
            }
            
            report.AddEntry(
                ExtraReportGroup.Stats,
                new GenericReportEntry(
                    label + " are found in/near the following shops: " + string.Join(", ", counters.Select(kvp => string.Join(" / ", kvp.Names.Select(n => "`" + n + "`")) + " (× " + kvp.Count + ")")) +
                    (unspecified > 0 ? " and " + unspecified + " unspecified" : "")
                )
            );
        }
    }
    
    
    private enum ExtraReportGroup
    {
        Stats
    }

    private class ShopCounter
    {
        public IEnumerable<string> Names => _names.AsReadOnly();

        public int Count { get; set; } = 1;

        
        private readonly List<string> _names = new List<string>();
        

        public ShopCounter(string firstName)
        {
            _names.Add(firstName);
        }


        public void Add(string anotherName)
        {
            Count++;
            if (!_names.Contains(anotherName))
                _names.Add(anotherName);
        }
    }
}