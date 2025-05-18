namespace Osmalyzer;

[DisabledAnalyzer("Map is dynamically loaded on navigating to tab, would need to implement browsing steps")]
public class MegoShopAnalyzer : ShopAnalyzer<MegoShopsAnalysisData, LatviaOsmAnalysisData>
{
    protected override string ShopName => "Mego";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}