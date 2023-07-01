using System;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class KuldigaRoadsAnalysisData : AnalysisData, ICachableAnalysisData
    {
        public override string Name => "Micro Reserves";

        public override string DataDateFileName => @"cache/kuldiga-roads-date.txt";

        public override bool? DataDateHasDayGranularity => false; // only day given on data page

        
        public string SubFolder => "cache/Kuldiga-roads";


        public DateTime RetrieveDataDate()
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

        protected override void Download()
        {
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
        }
    }
}