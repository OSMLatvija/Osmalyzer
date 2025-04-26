using System.Collections.Generic;

namespace Osmalyzer;

public class MaximaShopAnalyzer : ShopAnalyzer<MaximaShopsAnalysisData, LatviaOsmAnalysisData>
{
    protected override string ShopName => "Maxima";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName, ShopName + " X", ShopName + " XX", ShopName + " XXX", ShopName + " Hyper", ShopName + " express" };
}