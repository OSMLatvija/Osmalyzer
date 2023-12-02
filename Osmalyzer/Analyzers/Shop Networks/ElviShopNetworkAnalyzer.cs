using System.Collections.Generic;

namespace Osmalyzer;

public class ElviShopNetworkAnalyzer : ShopNetworkAnalyzer<ElviShopsAnalysisData>
{
    protected override string ShopName => "Elvi";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName, "Mazais " + ShopName, ShopName + " mazais" };
}