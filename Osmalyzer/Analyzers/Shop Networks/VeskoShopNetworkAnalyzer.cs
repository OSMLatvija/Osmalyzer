using System.Collections.Generic;

namespace Osmalyzer;

public class VeskoShopNetworkAnalyzer : ShopNetworkAnalyzer<VeskoShopsAnalysisData>
{
    protected override string ShopName => "Vesko";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}