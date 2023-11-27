using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class TopShopsAnalysisData : SimplePageShopListAnalysisData
{
    public override string Name => "Top! Shops";

    public override string ReportWebLink => @"https://www.toppartika.lv/veikali/";


    protected override string DataFileIdentifier => "shops-top";


    public override string DataFileName => cacheBasePath + DataFileIdentifier + @".html";

    public override string ShopListUrl => "https://www.toppartika.lv/veikali/";


    public override List<ShopData> GetShops()
    {
        // It's not in source, it's using google map with embedded data that I would need to somehow get
                
        throw new NotImplementedException();
    }
}