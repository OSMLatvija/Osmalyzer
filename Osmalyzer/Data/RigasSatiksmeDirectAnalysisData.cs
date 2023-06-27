using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    /// <summary>
    /// The data found on their website, seemingly whenever there are changes
    /// </summary>
    [UsedImplicitly]
    public class RigasSatiksmeDirectAnalysisData : PreparableAnalysisData
    {
        public override string Name => "Rigas Satiksme";
        
        public override string DataFileName => @"cache/rigas-satiksme-direct.zip";

        public override string DataDateFileName => @"cache/rigas-satiksme-direct.zip-date.txt";

        public override bool? DataDateHasDayGranularity => true; // only day given on data page (file itself is month only)

        
        public string ExtractionFolder => "RS";
        

        public override void Retrieve()
        {
            string url = "https://saraksti.rigassatiksme.lv/riga/gtfs.zip";

            // Check if we have a data file cached
            bool cachedFileOk = File.Exists(DataFileName);
            
            DateTime? newestDataDate = WebsiteDownloadHelper.ReadHeaderDate(url);

            if (newestDataDate == null)
            {
                Console.WriteLine("Data file did not have date header!");
                cachedFileOk = false;
            }
            
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
                    url, 
                    DataFileName
                );

                if (newestDataDate != null)
                    StoreDataDate(newestDataDate.Value);
                else
                    ClearDataDate(); // we couldn't get it
            }
        }

        public override void Prepare()
        {
            // RS data comes in a zip file, so unzip
            
            ZipHelper.ExtractZipFile(DataFileName, ExtractionFolder + "/");
        }
    }
}