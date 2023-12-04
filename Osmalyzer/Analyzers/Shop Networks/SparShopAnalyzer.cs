using System.Collections.Generic;

namespace Osmalyzer;

public class SparShopAnalyzer : ShopAnalyzer<SparShopsAnalysisData>
{
    protected override string ShopName => "Spar";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}