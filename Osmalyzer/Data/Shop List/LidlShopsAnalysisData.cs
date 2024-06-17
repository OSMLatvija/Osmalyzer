using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Osmalyzer;

[UsedImplicitly]
public class LidlShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Lidl Shops";

    public override string ReportWebLink => @"https://klientu-serviss.lidl.lv/SelfServiceLV/s/article/Kurās-pilsētās-7-oktobrī-tiks-atvērti-Lidl-veikali";
    //@"https://klientu-serviss.lidl.lv/SelfServiceLV/s/article/Kur%C4%81s-pils%C4%93t%C4%81s-7-oktobr%C4%AB-tiks-atv%C4%93rti-Lidl-veikali";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "shops-lidl";

    public override IEnumerable<ShopData> Shops => _shops;
    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteBrowsingHelper.DownloadPage(
            ReportWebLink, 
            Path.Combine(CacheBasePath, DataFileIdentifier + @".html"),
            true,
            new WaitForElementOfClass("siteforceContentArea") // loads JS garbage first that loads the rest of the page     siteforceContentArea  article_content
        );
    }

    protected override void DoPrepare()
    {
        string data = File.ReadAllText(Path.Combine(CacheBasePath, DataFileIdentifier + @".html"));

        //<a target="_blank" href="https://goo.gl/maps/6acQANjGsGkFyqFD9">Anniņmuižas bulvārī 77</a>

        _shops = new List<ShopData>();
        
        MatchCollection matches = Regex.Matches(data, @"<a target=""_blank"" href=""(https://(?:goo.gl/maps|maps.app.goo.gl)/[^""]+)""[^>]*>(.+?)</a>");

        if (matches.Count == 0)
            throw new Exception("Did not find items on webpage");
        
        foreach (Match match in matches)
        {
            // TODO: Move download to Download(), Prepare() should not be doing any web stuff or anything that can normally fail
            string? url = WebsiteDownloadHelper.GetRedirectUrl(match.Groups[1].Value);
            if (url == null)
                throw new Exception("Could not get redirect URL");
            
            Match m = Regex.Match(url, @"!3d(\d+\.\d+)!4d(\d+\.\d+)");
            OsmCoord coord = new OsmCoord(
                double.Parse(m.Groups[1].Value),
                double.Parse(m.Groups[2].Value)
            );

            ShopData sd = new ShopData(match.Groups[1].Value, match.Groups[2].Value, coord);

            _shops.Add(sd);
        }
    }
}