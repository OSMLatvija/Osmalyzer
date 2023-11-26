using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public abstract class ShopNetworkAnalyzer<T> : Analyzer where T : ShopListAnalysisData
{
    public override string Name => ShopName + " Shop Networks";

    public override string Description => "This report checks that all " + ShopName + " shops listed on brand's website are found on the map. " +
                                          "This supposes that brand shops are tagged correctly to match among multiple.";


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
            new HasKey("shop")
        );
        
        OsmDataExtract brandShops = osmShops.Filter(
            new OrMatch(
                new HasAnyValue("name", ShopOsmNames, false),
                new HasAnyValue("operator", ShopOsmNames, false),
                new HasAnyValue("brand", ShopOsmNames, false)
            )
        );

        // Load Shop data

        ShopListAnalysisData shopData = datas.OfType<ShopListAnalysisData>().First();

        List<ShopData> listedShops = shopData.GetShops();

        // Prepare data comparer/correlator

        Correlator<ShopData> dataComparer = new Correlator<ShopData>(
            brandShops,
            listedShops,
            new MatchDistanceParamater(15),
            new MatchFarDistanceParamater(75),
            new DataItemLabelsParamater(ShopName + " shop", ShopName + " shop")
        );
            
        // Parse and report primary matching and location correlation

        dataComparer.Parse(
            report,
            new MatchedItemBatch(),
            new UnmatchedItemBatch(),
            new MatchedFarItemBatch()
        );
    }
}