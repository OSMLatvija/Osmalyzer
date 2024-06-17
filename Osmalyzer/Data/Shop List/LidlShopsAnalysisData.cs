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

    
    private const string mainEntryMatchPattern = @"<a target=""_blank"" href=""(https://(?:goo\.gl/maps|maps\.app\.goo\.gl)/[^""]+)""[^>]*>(.+?)</a>";


    protected override void Download()
    {
        // Main page
        
        string data = WebsiteBrowsingHelper.Read(
            ReportWebLink, 
            true,
            null,
            new WaitForTime(5000) // loads JS garbage first that loads the rest of the page. No good way to identify when load finished
        );

        //File.WriteAllText(Path.Combine(CacheBasePath, DataFileIdentifier + @".html"), data);
        // Don't need it in Prepare at this time since we store the entry matches 
        
        // Urls/redirects from the main page's list
        
        MatchCollection matches = Regex.Matches(data, mainEntryMatchPattern);

        if (matches.Count == 0)
            throw new Exception("Did not find items on webpage");
        
        List<string> urls = new List<string>();
        
        foreach (Match match in matches)
        {
            string? url = WebsiteDownloadHelper.GetRedirectUrl(match.Groups[1].Value);
            if (url == null)
                throw new Exception("Could not get redirect URL");

            string matchSanitized = 
                match.ToString()
                     .Replace("\r\n","").Replace("\r","").Replace("\n","")
                     .Replace('\t',' '); // because of course it has tabs in it...
            
            urls.Add(matchSanitized + "\t" + url); // so we can match back in Prepare()
        }
        
        File.WriteAllLines(Path.Combine(CacheBasePath, DataFileIdentifier + @"-redirect-urls.tsv"), urls);
    }

    protected override void DoPrepare()
    {
        string[] urls = File.ReadAllLines(Path.Combine(CacheBasePath, DataFileIdentifier + @"-redirect-urls.tsv"));

        // <a target="_blank" href="https://goo.gl/maps/6acQANjGsGkFyqFD9">Anniņmuižas bulvārī 77</a>
        // might send us to 
        // https://www.google.com/maps/place/Lidl/@56.958092,23.940872,13z/data=!4m6!3m5!1s0x46eedb4812a3af97:0x4c3d59df18764c37!8m2!3d56.9580948!4d24.0109101!15sCgpMaWRsIHLEq2dhIgOIAQFaDCIKbGlkbCByxKtnYZIBFGRpc2NvdW50X3N1cGVybWFya2V0?shorturl=1

        _shops = new List<ShopData>();
        
        foreach (string url in urls)
        {
            if (url == "") 
                continue; // last newline
            
            string[] split = url.Split('\t');
            
            string mainMatchResult = split[0];
            Match mainMatch = Regex.Match(mainMatchResult, mainEntryMatchPattern);
            
            string redirectUrl = split[1];
            Match redirectUrlMatch = Regex.Match(redirectUrl, @"!3d(\d+\.\d+)!4d(\d+\.\d+)");
            
            OsmCoord coord = new OsmCoord(
                double.Parse(redirectUrlMatch.Groups[1].Value),
                double.Parse(redirectUrlMatch.Groups[2].Value)
            );

            ShopData sd = new ShopData(
                "Lidl", 
                mainMatch.Groups[2].Value, 
                coord
            );

            _shops.Add(sd);
        }
    }
}