using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    /// <summary>
    /// The data published in the open portal, once a month
    /// </summary>
    [UsedImplicitly]
    public class RigasSatiksmeOpenDataAnalysisData : AnalysisData, IPreparableAnalysisData
    {
        public override string Name => "Rigas Satiksme";

        public override string DataDateFileName => @"cache/rigas-satiksme.zip-date.txt";

        public override bool? DataDateHasDayGranularity => false; // only day given on data page (file itself is month only)

        
        public string ExtractionFolder => "RS";
        

        public override void OldRetrieve()
        {
            // Check if we have a data file cached
            bool cachedFileOk = File.Exists(@"cache/rigas-satiksme.zip");

            DateTime? newestDataDate = GetNewestRSDataDate(out string newestDataUrl);
            
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
                
                WebsiteDownloadHelper.Download(
                    newestDataUrl, 
                    @"cache/rigas-satiksme.zip"
                );

                StoreDataDate(newestDataDate.Value);
            }
            
            
            static DateTime GetNewestRSDataDate(out string dataUrl)
            {
                string result = WebsiteDownloadHelper.Read("https://data.gov.lv/dati/lv/dataset/marsrutu-saraksti-rigas-satiksme-sabiedriskajam-transportam", true);
                
                MatchCollection matches = Regex.Matches(result, @"<a href=""(https://data.gov.lv/dati/dataset/[a-f0-9\-]+/resource/[a-f0-9\-]+/download/marsrutusaraksti(\d{2})_(\d{4}).zip)""");
                Match urlMatch = matches.Last(); // last is latest... hopefully
                dataUrl = urlMatch.Groups[1].ToString();
                // todo: check if url date matches publish date? does it matter?

                Match dateMatch = Regex.Match(result, @"Datu pēdējo izmaiņu datums</th>\s*<td class=""dataset-details"">\s*(\d{4})-(\d{2})-(\d{2})");
                int newestYear = int.Parse(dateMatch.Groups[1].ToString());
                int newestMonth = int.Parse(dateMatch.Groups[2].ToString());
                int newestDay = int.Parse(dateMatch.Groups[3].ToString());
                return new DateTime(newestYear, newestMonth, newestDay);
            }
        }

        public void Prepare()
        {
            // RS data comes in a zip file, so unzip
            
            ZipHelper.ExtractZipFile(@"cache/rigas-satiksme.zip", ExtractionFolder + "/");
        }
    }
}