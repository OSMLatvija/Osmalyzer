using System.Collections.Generic;

namespace Osmalyzer;

public class HesburgerRestaurantAnalyzer : RestaurantAnalyzer<HesburgerRestaurantAnalysisData, LatviaOsmAnalysisData>
{
    protected override string RestaurantName => "Hesburger";

    protected override List<string> RestaurantOsmNames => new List<string>() { RestaurantName };
}