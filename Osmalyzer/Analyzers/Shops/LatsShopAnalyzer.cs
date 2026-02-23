namespace Osmalyzer;

public class LatsShopAnalyzer : ShopAnalyzer<LatsShopsAnalysisData, LatviaOsmAnalysisData>
{
    protected override string ShopName => "LaTS";

    protected override List<string> ShopOsmNames => new List<string>() { ShopName };
}