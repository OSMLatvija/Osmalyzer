using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class TopShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Top! Shops";

    public override string ReportWebLink => @"https://www.toppartika.lv/veikali/";


    protected override string DataFileIdentifier => "shops-top";


    public override string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://www.toppartika.lv/veikali/", 
            DataFileName
        );
    }

    public override void Prepare()
    {
        // It's not in source, it's using google map with embedded data that I would need to somehow get
                
        throw new NotImplementedException();
    }
}