namespace Osmalyzer;

public class MegoShopAnalyzer : ShopAnalyzer<MegoShopsAnalysisData, LatviaOsmAnalysisData>
{
    protected override string ShopName => "Mego";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}