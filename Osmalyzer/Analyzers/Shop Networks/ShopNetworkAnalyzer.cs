using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public abstract class ShopNetworkAnalyzer<T> : Analyzer where T : ShopListAnalysisData
{
    public override string Name => ShopName + " Shop Networks";

    public override string Description => "This report checks that all " + ShopName + " shops listed on brand's website are found on the map." + Environment.NewLine +
                                          "This supposes that brand shops are tagged correctly to match among multiple."  + Environment.NewLine +
                                          "Note that shop websites can and do have errors, mainly large offsets, but also missing or incorrect locations.";

    public override AnalyzerGroup Group => AnalyzerGroups.PublicTransport;


    protected abstract string ShopName { get; }

    protected abstract List<string> ShopOsmNames { get; }


    public override List<Type> GetRequiredDataTypes() => new List<Type>()
    {
        typeof(OsmAnalysisData), 
        typeof(T) // shop list data
    };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
                
        OsmDataExtract osmShops = osmMasterData.Filter(
            new HasAnyValue("shop", "yes", "supermarket", "grocery", "convenience")
        );
        
        OsmDataExtract brandShops = osmShops.Filter(
            new CustomMatch(ShopNameMatches)
        );

        bool ShopNameMatches(OsmElement osmElement)
        {
            // todo: use known brand data (file)

            string? osmName = osmElement.GetValue("name");

            if (osmName != null && ShopOsmNames.Any(sn => osmName.ToLower().Contains(sn.ToLower())))
                return true;

            string? osmOperator = osmElement.GetValue("operator");

            if (osmOperator != null && ShopOsmNames.Any(sn => osmOperator.ToLower().Contains(sn.ToLower())))
                return true;

            string? osmBrand = osmElement.GetValue("brand");

            if (osmBrand != null && ShopOsmNames.Any(sn => osmBrand.ToLower().Contains(sn.ToLower())))
                return true;
            
            return false;
        }

        // Load Shop data

        ShopListAnalysisData shopData = datas.OfType<ShopListAnalysisData>().First();

        List<ShopData> listedShops = shopData.Shops.ToList();

        // Prepare data comparer/correlator

        Correlator<ShopData> dataComparer = new Correlator<ShopData>(
            brandShops,
            listedShops,
            new MatchDistanceParamater(100),
            new MatchFarDistanceParamater(300), // some are really far from where the data says they ought to be
            new MatchExtraDistanceParamater(MatchStrength.Strong, 700), // allow really far for exact matches
            new DataItemLabelsParamater(ShopName + " shop", ShopName + " shops"),
            new OsmElementPreviewValue("name", false),
            new LoneElementAllowanceCallbackParameter(_ => true),
            new MatchCallbackParameter<ShopData>(GetMatchStrength)
        );
        
        // todo: report closest potential (brand-untagged) shop when not matching anything?

        [Pure]
        MatchStrength GetMatchStrength(ShopData point, OsmElement element)
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