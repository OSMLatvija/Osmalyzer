using System.Collections.Generic;

namespace Osmalyzer;

public class LidlShopAnalyzer : ShopAnalyzer<LidlShopsAnalysisData, LatviaOsmAnalysisData>
{
    protected override string ShopName => "Lidl";

    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}