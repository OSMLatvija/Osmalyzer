using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class MicroReserveAnalysisData : AnalysisData, IPreparableAnalysisData, ICachableAnalysisData
    {
        public override string Name => "Micro Reserves";

        public bool DataDateHasDayGranularity => false; // only day given on data page

        protected override string DataFileIdentifier => "micro-reserves";

        
        public string ExtractionFolder => "MR";


        public DateTime RetrieveDataDate()
        {
            string result = WebsiteDownloadHelper.Read("https://data.gov.lv/dati/lv/dataset/mikroliegumi", true);

            Match dateMatch = Regex.Match(result, @"Datu pēdējo izmaiņu datums</th>\s*<td class=""dataset-details"">\s*(\d{4})-(\d{2})-(\d{2})");
            int newestYear = int.Parse(dateMatch.Groups[1].ToString());
            int newestMonth = int.Parse(dateMatch.Groups[2].ToString());
            int newestDay = int.Parse(dateMatch.Groups[3].ToString());
                
            return new DateTime(newestYear, newestMonth, newestDay);
        }

        protected override void Download()
        {
            

            string result = WebsiteDownloadHelper.Read("https://data.gov.lv/dati/lv/dataset/mikroliegumi", true);

            Match urlMatch = Regex.Match(result, @"<a class=""heading"" href=""(/dati/lv/dataset/mikroliegumi/resource/[^""]+)"" title=""mikroliegumi"">");

            string url = @"https://data.gov.lv" + urlMatch.Groups[1];

            result = WebsiteDownloadHelper.Read(url, true);

            urlMatch = Regex.Match(result, @"URL: <a href=""([^""]+)""");

            url = urlMatch.Groups[1].ToString();

            WebsiteDownloadHelper.Download(
                url,
                cacheBasePath + @"micro-reserves.zip"
            );
        }

        public void Prepare()
        {
            // Data comes in a zip file, so unzip
            
            ZipHelper.ExtractZipFile(cacheBasePath + DataFileIdentifier + @".zip", ExtractionFolder + "/");
        }
    }
}