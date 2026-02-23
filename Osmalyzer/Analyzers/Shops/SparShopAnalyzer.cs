namespace Osmalyzer;

public class SparShopAnalyzer : ShopAnalyzer<SparShopsAnalysisData, LatviaOsmAnalysisData>
{
    protected override string ShopName => "Spar";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}