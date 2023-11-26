using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class MaximaShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Maxima Shops";

    protected override string DataFileIdentifier => "shops-maxima";

    public override string DataFileName => cacheBasePath + DataFileIdentifier + @".html";

    
    protected override void Download()
    {
        // list at https://www.maxima.lv/veikalu-kedes
        // json query at https://www.maxima.lv/ajax/shopsnetwork/map/getCities
        
        // Default POST query with all items:
        // {
        //     "cityId": "0",
        //     "shopType": "",
        //     "mapId": "1",
        //     "shopId": "",
        //     "language": "lv_lv",
        //     "certificate": ""
        // }

        WebsiteDownloadHelper.DownloadPost(
            @"https://www.maxima.lv/ajax/shopsnetwork/map/getCities",
            new[] { ("cityId", "0"), ("shopType", ""), ("mapId", "1"), ("shopId", ""), ("language", "lv_lv"), ("certificate", "") },
            DataFileName
        );
    }


    public override List<ShopData> GetShops()
    {
        //
                
        string source = File.ReadAllText(DataFileName);
        
        List<ShopData> listedShops = new List<ShopData>();
                
        throw new NotImplementedException();
    }
}