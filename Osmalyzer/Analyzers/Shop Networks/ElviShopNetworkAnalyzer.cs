using System.Collections.Generic;

namespace Osmalyzer;

public class ElviShopNetworkAnalyzer : ShopNetworkAnalyzer<ElviShopsAnalysisData>
{
    protected override string ShopName => "Elvi";

    public override AnalyzerGroup Group => AnalyzerGroups.Shop;
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName, "Mazais " + ShopName, ShopName + " mazais" };
}