using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Osmalyzer;

/// <summary>
/// The data published in the open portal, once a month
/// </summary>
[UsedImplicitly]
public class RigasSatiksmeOpenDataAnalysisData : AnalysisData, IDatedAnalysisData
{
    public override string Name => "Rigas Satiksme";

    public override string ReportWebLink => @"https://data.gov.lv/dati/lv/dataset/marsrutu-saraksti-rigas-satiksme-sabiedriskajam-transportam";


    public bool DataDateHasDayGranularity => false; // only day given on data page (file itself is month only)

    protected override string DataFileIdentifier => "rigas-satiksme-opendata";

    public override bool NeedsPreparation => true;

        
    public string ExtractionFolder => "RS";


    public DateTime RetrieveDataDate()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct download/scraping
            "https://data.gov.lv/dati/lv/dataset/marsrutu-saraksti-rigas-satiksme-sabiedriskajam-transportam", 
            true
        );

        Match dateMatch = Regex.Match(result, @"Datu pēdējo izmaiņu datums</th>\s*<td class=""dataset-details"">\s*(\d{4})-(\d{2})-(\d{2})");
        int newestYear = int.Parse(dateMatch.Groups[1].ToString());
        int newestMonth = int.Parse(dateMatch.Groups[2].ToString());
        int newestDay = int.Parse(dateMatch.Groups[3].ToString());
            
        // todo: check if url date matches publish date? does it matter?

        return new DateTime(newestYear, newestMonth, newestDay);
    }

    protected override void Download()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct download/scraping
            "https://data.gov.lv/dati/lv/dataset/marsrutu-saraksti-rigas-satiksme-sabiedriskajam-transportam", 
            true
        );
        
        MatchCollection matches = Regex.Matches(result, @"<a href=""(https://data.gov.lv/dati/dataset/[a-f0-9\-]+/resource/[a-f0-9\-]+/download/marsrutusaraksti(\d{2})_(\d{4}).zip)""");
        Match urlMatch = matches.Last(); // last is latest... hopefully
        string dataUrl = urlMatch.Groups[1].ToString();
            
        WebsiteDownloadHelper.Download(
            dataUrl, 
            Path.Combine(CacheBasePath, DataFileIdentifier + @".zip")
        );
    }

    protected override void DoPrepare()
    {
        // RS data comes in a zip file, so unzip
            
        ZipHelper.ExtractZipFile(
            Path.Combine(CacheBasePath, DataFileIdentifier + @".zip"), 
            ExtractionFolder
        );
    }
}