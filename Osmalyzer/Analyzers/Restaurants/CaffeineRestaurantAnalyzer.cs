namespace Osmalyzer;

public class CaffeineRestaurantAnalyzer : RestaurantAnalyzer<CaffeineRestaurantAnalysisData, LatviaOsmAnalysisData>
{
    protected override string RestaurantName => "Caffeine";

    protected override List<string> RestaurantOsmNames => new List<string>() { RestaurantName };
}