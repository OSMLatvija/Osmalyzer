﻿using System.Collections.Generic;

namespace Osmalyzer;

public class RimiShopAnalyzer : ShopAnalyzer<RimiShopsAnalysisData>
{
    protected override string ShopName => "Rimi";
    
    protected override List<string> ShopOsmNames => new List<string>() { ShopName, ShopName + " Mini" };
}