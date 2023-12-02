using System.Collections.Generic;

namespace Osmalyzer;

public class TopShopNetworkAnalyzer : ShopNetworkAnalyzer<TopShopsAnalysisData>
{
    protected override string ShopName => "Top!";

    protected override List<string> ShopOsmNames => new List<string>() { ShopName, "Top", "Mini" + ShopName, "Mini top" };
}