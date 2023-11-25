using System.Collections.Generic;

namespace Osmalyzer;

public class LatsShopNetworkAnalyzer : ShopNetworkAnalyzer<LatsShopsAnalysisData>
{
    protected override string ShopName => "LaTS";

    protected override List<string> ShopOsmNames => new List<string>() { Name };
}