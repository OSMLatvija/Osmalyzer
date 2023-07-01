using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class MicroReserveAnalysisData : PreparableAnalysisData
    {
        public override string Name => "Micro Reserves";

        public override string DataDateFileName => @"cache/micro-reserves.zip-date.txt";

        public override bool? DataDateHasDayGranularity => false; // only day given on data page

        
        public string ExtractionFolder => "MR";
        

        public override void Retrieve()
        {
            // Check if we have a data file cached
            bool cachedFileOk = File.Exists(@"cache/micro-reserves.zip");

            DateTime? newestDataDate = GetNewestDataDate();
            
            if (cachedFileOk)
            {
                // Check that we actually know the date it was cached

                if (DataDate == null)
                {
                    Console.WriteLine("Missing data date metafile!");
                    cachedFileOk = false;
                }
            }

            if (cachedFileOk)
            {
                // Check that we have the latest date

                if (DataDate < newestDataDate)
                {
                    Console.WriteLine("Cached data out of date!");
                    cachedFileOk = false;
                }
            }
            
            if (!cachedFileOk)
            {
                // Download latest (if anything is wrong)
             
                Console.WriteLine("Downloading...");
                
                string result = WebsiteDownloadHelper.Read("https://data.gov.lv/dati/lv/dataset/mikroliegumi", true);

                Match urlMatch = Regex.Match(result, @"<a class=""heading"" href=""(/dati/lv/dataset/mikroliegumi/resource/[^""]+)"" title=""mikroliegumi"">");

                string url = @"https://data.gov.lv" + urlMatch.Groups[1];
                    
                result = WebsiteDownloadHelper.Read(url, true);
                
                urlMatch = Regex.Match(result, @"URL: <a href=""([^""]+)""");

                url = urlMatch.Groups[1].ToString();
                
                WebsiteDownloadHelper.Download(
                    url,
                    @"cache/micro-reserves.zip"
                );

                StoreDataDate(newestDataDate.Value);
            }
            
            
            static DateTime GetNewestDataDate()
            {
                string result = WebsiteDownloadHelper.Read("https://data.gov.lv/dati/lv/dataset/mikroliegumi", true);

                Match dateMatch = Regex.Match(result, @"Datu pēdējo izmaiņu datums</th>\s*<td class=""dataset-details"">\s*(\d{4})-(\d{2})-(\d{2})");
                int newestYear = int.Parse(dateMatch.Groups[1].ToString());
                int newestMonth = int.Parse(dateMatch.Groups[2].ToString());
                int newestDay = int.Parse(dateMatch.Groups[3].ToString());
                return new DateTime(newestYear, newestMonth, newestDay);
            }
        }

        public override void Prepare()
        {
            // Data comes in a zip file, so unzip
            
            ZipHelper.ExtractZipFile(@"cache/micro-reserves.zip", ExtractionFolder + "/");
        }
    }
}