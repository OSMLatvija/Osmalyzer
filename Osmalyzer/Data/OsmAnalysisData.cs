using System;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class OsmAnalysisData : AnalysisData
    {
        public override string Name => "OSM";
        
        public override string DataFileName => @"cache/latvia-latest.osm.pbf";

        public override string DataDateFileName => @"cache/latvia-latest.osm.pbf-date.txt";

        public override bool? DataDateHasDayGranularity => true;
        

        public override void Retrieve()
        {
            // Check if we have a data file cached
            bool cachedFileOk = File.Exists(DataFileName);

            DateTime? newestDataDate = GetNewestOsmDataDate();

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
                    "https://download.geofabrik.de/europe/latvia-latest.osm.pbf", 
                    DataFileName
                );

                StoreDataDate(newestDataDate.Value);
            }
            
            
            static DateTime GetNewestOsmDataDate()
            {
                string result = WebsiteDownloadHelper.Read("https://download.geofabrik.de/europe/latvia.html", true);
                
                Match match = Regex.Match(result, @"contains all OSM data up to ([^\.]+)\.");
                string newestDateString = match.Groups[1].ToString(); // will be something like "2023-06-12T20:21:53Z"
                return DateTime.Parse(newestDateString);
            }
        }


        public override void Prepare()
        {
            // Don't need to prepare anything
        }
    }
}