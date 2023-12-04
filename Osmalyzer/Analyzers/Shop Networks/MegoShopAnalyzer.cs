using System.Collections.Generic;

namespace Osmalyzer;

public class MegoShopAnalyzer : ShopAnalyzer<MegoShopsAnalysisData>
{
    protected override string ShopName => "Mego";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}