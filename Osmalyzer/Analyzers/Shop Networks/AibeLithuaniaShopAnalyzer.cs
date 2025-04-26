using System.Collections.Generic;

namespace Osmalyzer;

[DisabledAnalyzer]
public class AibeLithuaniaShopAnalyzer : ShopAnalyzer<AibeLithuaniaShopsAnalysisData, LithuaniaOsmAnalysisData>
{
    protected override string ShopName => "Aibė";
    
    protected override string ShopNameDisambiguator => "Lithuania";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}