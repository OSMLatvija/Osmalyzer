namespace Osmalyzer;

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