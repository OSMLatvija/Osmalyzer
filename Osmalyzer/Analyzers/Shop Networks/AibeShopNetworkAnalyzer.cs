using System.Collections.Generic;

namespace Osmalyzer;

public class AibeShopNetworkAnalyzer : ShopNetworkAnalyzer<AibeShopsAnalysisData>
{
    protected override string ShopName => "Aibe";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}