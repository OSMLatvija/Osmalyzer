using System.Collections.Generic;

namespace Osmalyzer;

public class LatsShopAnalyzer : ShopAnalyzer<LatsShopsAnalysisData>
{
    protected override string ShopName => "LaTS";

    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}