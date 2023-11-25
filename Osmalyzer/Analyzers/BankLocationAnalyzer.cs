using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class BankLocationAnalyzer : Analyzer
{
    public override string Name => "Bank Locations";

    public override string Description => "This report checks that all POIs from bank lists are mapped.";

    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(SwedbankPointAnalysisData) };


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract allOsmPoints = osmMasterData.Filter(
            new HasAnyValue("amenity", "atm", "bank")
        );

        OsmDataExtract osmAtms = allOsmPoints.Filter(
            new HasValue("amenity", "atm")
        );

        OsmDataExtract osmBranches = allOsmPoints.Filter(
            new HasValue("amenity", "bank")
        );

        // Get Bank data

        List<BankPoint> allPoints = datas.OfType<SwedbankPointAnalysisData>().First().Points;

        List<BankAtmPoint> atmPoints = allPoints.OfType<BankAtmPoint>().ToList();

        List<BankBranchPoint> branchPoints = allPoints.OfType<BankBranchPoint>().ToList();

        // Correlate

        Correlate(osmAtms, atmPoints, "ATM", "ATMs");
        
        Correlate(osmBranches, branchPoints, "branch", "branches");
        

        void Correlate<T>(OsmDataExtract osmPoints, List<T> dataPoints, string labelSignular, string labelPlural) where T : BankPoint
        {
            // Prepare data comparer/correlator

            Correlator<T> dataComparer = new Correlator<T>(
                osmPoints,
                dataPoints,
                new MatchDistanceParamater(50),
                new MatchFarDistanceParamater(300), // some are stupidly far, like at the opposite end of a shopping center from the website's point
                new DataItemLabelsParamater("Swedbank " + labelSignular, "Swedbank " + labelPlural),
                new MatchCallbackParameter<T>(DoesOsmPointMatchBankPoint)
            );

            [Pure]
            static bool DoesOsmPointMatchBankPoint(BankPoint point, OsmElement osmElement)
            {
                string? osmName =
                    osmElement.GetValue("operator") ??
                    osmElement.GetValue("brand") ??
                    osmElement.GetValue("name") ??
                    null;

                return osmName != null && osmName.ToLower().Contains("swedbank");
            }

            // Parse and report primary matching and location correlation

            dataComparer.Parse(
                report,
                new MatchedItemBatch(),
                new UnmatchedItemBatch(),
                new MatchedFarItemBatch()
            );
        }
    }
}