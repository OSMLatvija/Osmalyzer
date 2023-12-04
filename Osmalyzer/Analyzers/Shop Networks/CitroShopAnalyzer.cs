using System.Collections.Generic;

namespace Osmalyzer;

public class CitroShopAnalyzer : ShopAnalyzer<CitroShopsAnalysisData>
{
    protected override string ShopName => "Citro";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName, ShopName + " MINI" };
}