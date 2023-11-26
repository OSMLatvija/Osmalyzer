using System;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer;

/// <summary>
/// The data published in the open portal, once a month
/// </summary>
[UsedImplicitly]
public class RigasSatiksmeOpenDataAnalysisData : AnalysisData, IPreparableAnalysisData, ICachableAnalysisData
{
    public override string Name => "Rigas Satiksme";

    public bool DataDateHasDayGranularity => false; // only day given on data page (file itself is month only)

    protected override string DataFileIdentifier => "rigas-satiksme-opendata";

        
    public string ExtractionFolder => "RS";


    public DateTime RetrieveDataDate()
    {
        string result = WebsiteDownloadHelper.ReadDirect("https://data.gov.lv/dati/lv/dataset/marsrutu-saraksti-rigas-satiksme-sabiedriskajam-transportam", true);

        Match dateMatch = Regex.Match(result, @"Datu pēdējo izmaiņu datums</th>\s*<td class=""dataset-details"">\s*(\d{4})-(\d{2})-(\d{2})");
        int newestYear = int.Parse(dateMatch.Groups[1].ToString());
        int newestMonth = int.Parse(dateMatch.Groups[2].ToString());
        int newestDay = int.Parse(dateMatch.Groups[3].ToString());
            
        // todo: check if url date matches publish date? does it matter?

        return new DateTime(newestYear, newestMonth, newestDay);
    }

    protected override void Download()
    {
        string result = WebsiteDownloadHelper.ReadDirect("https://data.gov.lv/dati/lv/dataset/marsrutu-saraksti-rigas-satiksme-sabiedriskajam-transportam", true);
        
        MatchCollection matches = Regex.Matches(result, @"<a href=""(https://data.gov.lv/dati/dataset/[a-f0-9\-]+/resource/[a-f0-9\-]+/download/marsrutusaraksti(\d{2})_(\d{4}).zip)""");
        Match urlMatch = matches.Last(); // last is latest... hopefully
        string dataUrl = urlMatch.Groups[1].ToString();
            
        WebsiteDownloadHelper.Download(
            dataUrl, 
            cacheBasePath + DataFileIdentifier + @".zip"
        );
    }

    public void Prepare()
    {
        // RS data comes in a zip file, so unzip
            
        ZipHelper.ExtractZipFile(cacheBasePath + DataFileIdentifier + @".zip", ExtractionFolder + "/");
    }
}