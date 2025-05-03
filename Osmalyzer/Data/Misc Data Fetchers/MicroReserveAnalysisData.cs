namespace Osmalyzer;

[UsedImplicitly]
public class MicroReserveAnalysisData : AnalysisData, IDatedAnalysisData
{
    public override string Name => "Micro Reserves";

    public override string ReportWebLink => @"https://data.gov.lv/dati/lv/dataset/mikroliegumi";

    public override bool NeedsPreparation => true;


    public bool DataDateHasDayGranularity => false; // only day given on data page

    protected override string DataFileIdentifier => "micro-reserves";

        
    public string ExtractionFolder => "MR";


    public DateTime RetrieveDataDate()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct download/scraping
            "https://data.gov.lv/dati/lv/dataset/mikroliegumi", 
            true
        );

        Match dateMatch = Regex.Match(result, @"Datu pēdējo izmaiņu datums</th>\s*<td class=""dataset-details"">\s*(\d{4})-(\d{2})-(\d{2})");
        int newestYear = int.Parse(dateMatch.Groups[1].ToString());
        int newestMonth = int.Parse(dateMatch.Groups[2].ToString());
        int newestDay = int.Parse(dateMatch.Groups[3].ToString());
                
        return new DateTime(newestYear, newestMonth, newestDay);
    }

    protected override void Download()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct download/scraping
            "https://data.gov.lv/dati/lv/dataset/mikroliegumi", 
            true
        );

        Match urlMatch = Regex.Match(result, @"<a class=""heading"" href=""(/dati/lv/dataset/mikroliegumi/resource/[^""]+)"" title=""mikroliegumi"">");

        string url = @"https://data.gov.lv" + urlMatch.Groups[1];

        result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct download/scraping
            url, 
            true
        );

        urlMatch = Regex.Match(result, @"URL: <a href=""([^""]+)""");

        url = urlMatch.Groups[1].ToString();

        WebsiteDownloadHelper.Download(
            url,
            Path.Combine(CacheBasePath, @"micro-reserves.zip")
        );
    }

    protected override void DoPrepare()
    {
        // Data comes in a zip file, so unzip
            
        ZipHelper.ExtractZipFile(
            Path.Combine(CacheBasePath, DataFileIdentifier + @".zip"),
            Path.GetFullPath(ExtractionFolder)
        );
        
        // Make sure the files are in root and not in subfolder, as the data start being suddenly (e.g. "Mikroliegumi_20.12.2023")
        
        string[] subfolders = Directory.GetDirectories(ExtractionFolder);
        
        if (subfolders.Length == 1)
        {
            string[] subfolderFiles = Directory.GetFiles(subfolders[0]);
            
            foreach (string file in subfolderFiles)
            {
                string destination = Path.Combine(ExtractionFolder, Path.GetFileName(file));
                File.Move(file, destination);
            }

            Directory.Delete(subfolders[0]);
        }
    }
}