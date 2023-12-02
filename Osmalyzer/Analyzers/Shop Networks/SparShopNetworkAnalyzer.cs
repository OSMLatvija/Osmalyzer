using System.Collections.Generic;

namespace Osmalyzer;

public class SparShopNetworkAnalyzer : ShopNetworkAnalyzer<SparShopsAnalysisData>
{
    protected override string ShopName => "Spar";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}