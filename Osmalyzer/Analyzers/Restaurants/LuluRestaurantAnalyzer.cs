using System.Collections.Generic;

namespace Osmalyzer;

public class LuluRestaurantAnalyzer : RestaurantAnalyzer<LuluRestaurantAnalysisData, LatviaOsmAnalysisData>
{
    protected override string RestaurantName => "Lulu";

    protected override List<string> RestaurantOsmNames => new List<string>() { RestaurantName, "LuLū pica", "Pica LuLū", "Lulū pizza" };
}