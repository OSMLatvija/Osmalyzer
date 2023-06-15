using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Osmalyzer
{
    public class RoadLawAnalysisData : AnalysisData
    {
        public override string Name => "Road Law";
        
        public override string DataFileName => @"cache/road-law.html";

        public override string? DataDateFileName => null;

        public override bool? DataDateHasDayGranularity => null;


        public override void Retrieve()
        {
            // Download latest (if anything is wrong)

            using HttpClient client = new HttpClient();
            using Task<Stream> stream = client.GetStreamAsync("https://likumi.lv/ta/id/198589-noteikumi-par-valsts-autocelu-un-valsts-autocelu-maroadLawruta-ietverto-pasvaldibam-piederoso-autocelu-posmu-sarakstiem");
            using FileStream fileStream = new FileStream(DataFileName, FileMode.Create);
            stream.Result.CopyTo(fileStream);
        }

        public override void Prepare()
        {
            // Don't need to prepare anything
        }
    }
}