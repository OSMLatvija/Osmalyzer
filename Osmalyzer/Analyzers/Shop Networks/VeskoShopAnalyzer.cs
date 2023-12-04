using System.Collections.Generic;

namespace Osmalyzer;

public class VeskoShopAnalyzer : ShopAnalyzer<VeskoShopsAnalysisData>
{
    protected override string ShopName => "Vesko";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}