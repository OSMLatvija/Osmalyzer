#if !REMOTE_EXECUTION

using System.Collections.Generic;

namespace Osmalyzer;

public class TopShopNetworkAnalyzer : ShopNetworkAnalyzer<LatsShopsAnalysisData>
{
    protected override string ShopName => "Top!";

    protected override List<string> ShopOsmNames => new List<string>() { ShopName, "Top" };
}

#endif
