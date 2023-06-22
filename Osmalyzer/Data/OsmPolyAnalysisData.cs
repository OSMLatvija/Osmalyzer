using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class OsmPolyAnalysisData : AnalysisData
    {
        public override string Name => "OSM Poly";
        
        public override string DataFileName => @"cache/latvia.poly";

        public override string DataDateFileName => @"cache/latvia.poly-date.txt";

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
                
                using HttpClient client = new HttpClient();
                using Task<Stream> stream = client.GetStreamAsync("https://download.geofabrik.de/europe/latvia.poly");
                using FileStream fileStream = new FileStream(DataFileName, FileMode.Create);
                stream.Result.CopyTo(fileStream);

                StoreDataDate(newestDataDate.Value);
            }
            
            
            static DateTime GetNewestOsmDataDate()
            {
                string url = "https://download.geofabrik.de/europe/latvia.html";
                using HttpClient client = new HttpClient();
                using HttpResponseMessage response = client.GetAsync(url).Result;
                using HttpContent content = response.Content;
                string result = content.ReadAsStringAsync().Result;
                
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