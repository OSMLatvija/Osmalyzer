using System;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class KuldigaRoadsAnalysisData : PreparableAnalysisData
    {
        public override string Name => "Micro Reserves";

        public override string DataDateFileName => @"cache/kuldiga-roads-date.txt";

        public override bool? DataDateHasDayGranularity => false; // only day given on data page

        
        public string SubFolder => "cache/Kuldiga-roads";
        

        public override void Retrieve()
        {
            // todo: chceck actual file list matches

            bool cachedDateOk = true;
            
            DateTime? newestDataDate = GetNewestDataDate();
            
            // Check that we actually know the date it was cached

            if (DataDate == null)
            {
                Console.WriteLine("Missing data date metafile!");
                cachedDateOk = false;
            }

            if (cachedDateOk)
            {
                // Check that we have the latest date

                if (DataDate < newestDataDate)
                {
                    Console.WriteLine("Cached data out of date!");
                    cachedDateOk = false;
                }
            }
            
            if (!cachedDateOk)
            {
                // Download latest (if anything is wrong)
             
                Console.WriteLine("Downloading...");

                if (!Directory.Exists(SubFolder))
                    Directory.CreateDirectory(SubFolder);
                
                string result = WebsiteDownloadHelper.Read("https://www.kuldiga.lv/pasvaldiba/publiskie-dokumenti/autocelu-klases", true);

                MatchCollection urlMatches = Regex.Matches(result, @"<a href=""(/images/Faili/Pasvaldiba/autoceli/([^""]+))"">");

                foreach (Match urlMatch in urlMatches)
                {
                    string url = @"https://www.kuldiga.lv" + urlMatch.Groups[1];

                    WebsiteDownloadHelper.Download(
                        url,
                        SubFolder + "/" + urlMatch.Groups[2]
                    );
                }

                StoreDataDate(newestDataDate.Value);
            }
            
            
            static DateTime GetNewestDataDate()
            {
                string result = WebsiteDownloadHelper.Read("https://www.kuldiga.lv/pasvaldiba/publiskie-dokumenti/autocelu-klases", true);

                Match dateMatch = Regex.Match(result, @"Publicēts (\d{1,2})\.(\d{1,2})\.(\d{1,4})\s*(\d{1,2}):(\d{1,2})");
                int newestYear = int.Parse(dateMatch.Groups[3].ToString());
                int newestMonth = int.Parse(dateMatch.Groups[2].ToString());
                int newestDay = int.Parse(dateMatch.Groups[1].ToString());
                int newestHour = int.Parse(dateMatch.Groups[4].ToString());
                int newestMinute = int.Parse(dateMatch.Groups[5].ToString());
                return new DateTime(newestYear, newestMonth, newestDay, newestHour, newestMinute, 0);
            }
        }

        public override void Prepare()
        {
            
        }
    }
}