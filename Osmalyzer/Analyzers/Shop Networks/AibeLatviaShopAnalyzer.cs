using System.Collections.Generic;

namespace Osmalyzer;

public class AibeLatviaShopAnalyzer : ShopAnalyzer<AibeLatviaShopsAnalysisData, LatviaOsmAnalysisData>
{
    protected override string ShopName => "Aibe";
    
    protected override string ShopNameDisambiguator => "Latvia";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}