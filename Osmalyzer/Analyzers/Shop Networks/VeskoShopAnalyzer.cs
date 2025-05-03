namespace Osmalyzer;

public class VeskoShopAnalyzer : ShopAnalyzer<VeskoShopsAnalysisData, LatviaOsmAnalysisData>
{
    protected override string ShopName => "Vesko";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}