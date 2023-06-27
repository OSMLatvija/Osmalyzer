using System;
using System.IO;

namespace Osmalyzer
{
    public abstract class GTFSAnalysisData : PreparableAnalysisData
    {
        protected abstract string DataURL { get; }
        
        protected abstract string ExtractionFolder { get; }

        
        public override void Retrieve()
        {
            // Check if we have a data file cached
            bool cachedFileOk = File.Exists(DataFileName);
            
            DateTime? newestDataDate = WebsiteDownloadHelper.ReadHeaderDate(DataURL);

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
                    DataURL, 
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