namespace Osmalyzer;

public class RimiShopAnalyzer : ShopAnalyzer<RimiShopsAnalysisData, LatviaOsmAnalysisData>
{
    protected override string ShopName => "Rimi";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName, ShopName + " Mini" };
}