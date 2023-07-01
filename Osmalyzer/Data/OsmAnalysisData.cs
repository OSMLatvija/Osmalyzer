using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class OsmAnalysisData : AnalysisData, IPreparableAnalysisData, ICachableAnalysisData
    {
        public override string Name => "OSM";

        public override string DataDateFileName => @"cache/latvia-latest.osm.pbf-date.txt";

        public override bool? DataDateHasDayGranularity => true;


        public OsmMasterData MasterData { get; private set; } = null!; // only null during initialization


        public DateTime RetrieveDataDate()
        {
            string result = WebsiteDownloadHelper.Read("https://download.geofabrik.de/europe/latvia.html", true);
                
            Match match = Regex.Match(result, @"contains all OSM data up to ([^\.]+)\.");
            string newestDateString = match.Groups[1].ToString(); // will be something like "2023-06-12T20:21:53Z"
            
            return DateTime.Parse(newestDateString);
        }

        protected override void Download()
        {
            
            
            WebsiteDownloadHelper.Download(
                "https://download.geofabrik.de/europe/latvia-latest.osm.pbf", 
                @"cache/latvia-latest.osm.pbf"
            );
        }

        public void Prepare()
        {
            MasterData = new OsmMasterData(@"cache/latvia-latest.osm.pbf");
        }
    }
}