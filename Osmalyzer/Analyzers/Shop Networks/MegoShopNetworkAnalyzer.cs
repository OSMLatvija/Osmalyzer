using System.Collections.Generic;

namespace Osmalyzer;

public class MegoShopNetworkAnalyzer : ShopNetworkAnalyzer<MegoShopsAnalysisData>
{
    protected override string ShopName => "Mego";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}