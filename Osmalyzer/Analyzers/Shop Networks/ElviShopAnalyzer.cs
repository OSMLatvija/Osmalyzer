﻿using System.Collections.Generic;

namespace Osmalyzer;

public class ElviShopAnalyzer : ShopAnalyzer<ElviShopsAnalysisData>
{
    protected override string ShopName => "Elvi";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName, "Mazais " + ShopName, ShopName + " mazais" };
}