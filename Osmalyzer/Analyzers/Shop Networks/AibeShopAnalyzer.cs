using System.Collections.Generic;

namespace Osmalyzer;

public class AibeShopAnalyzer : ShopAnalyzer<AibeShopsAnalysisData>
{
    protected override string ShopName => "Aibe";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}