﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public abstract class BankLocationAnalyzer<TData> : Analyzer where TData : BankPointAnalysisData
{
    public override string Name => BankName + " Locations";

    public override string Description => "This report checks that all POIs from " + BankName + " contact list are mapped. " +
                                          "Note that the website list is not precise and the actual points can be dozens and even hundreds of meters away, such as in shopping malls.";

    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(TData) };


    protected abstract string BankName { get; }


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract allOsmPoints = osmMasterData.Filter(
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

        OsmDataExtract osmAtms = allOsmPoints.Filter(
            new HasValue("amenity", "atm")
        );

        OsmDataExtract osmBranches = allOsmPoints.Filter(
            new HasValue("amenity", "bank")
        );

        // Get Bank data

        List<BankPoint> allPoints = datas.OfType<BankPointAnalysisData>().First().Points;
        
        List<BankAtmPoint> atmPoints = allPoints.OfType<BankAtmPoint>().ToList();

        List<BankBranchPoint> branchPoints = allPoints.OfType<BankBranchPoint>().ToList();

        // Correlate

        Correlate(osmAtms, atmPoints, "ATM", "ATMs");
        
        Correlate(osmBranches, branchPoints, "branch", "branches");
        

        void Correlate<TItem>(OsmDataExtract osmPoints, List<TItem> dataPoints, string labelSingular, string labelPlural) where TItem : BankPoint
        {
            // Prepare data comparer/correlator

            Correlator<TItem> dataComparer = new Correlator<TItem>(
                osmPoints,
                dataPoints,
                new MatchDistanceParamater(50),
                new MatchFarDistanceParamater(300), // some are stupidly far, like at the opposite end of a shopping center from the website's point
                new DataItemLabelsParamater(BankName + " " + labelSingular, BankName + " " + labelPlural),
                new LoneElementAllowanceCallbackParameter(_ => false)// all our points represent a POI supposedly for this bank, none can be standalone 
            );

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