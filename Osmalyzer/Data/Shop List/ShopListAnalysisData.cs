using System.Collections.Generic;

namespace Osmalyzer;

public abstract class ShopListAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public abstract string DataFileName { get; }
    
    
    public abstract List<ShopData> GetShops();
}

public abstract class SimplePageShopListAnalysisData : ShopListAnalysisData
{
    public abstract string ShopListUrl { get; }


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            ShopListUrl, 
            DataFileName
        );
    }
}