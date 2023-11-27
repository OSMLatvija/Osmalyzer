using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class CulturalMonumentsAnalysisData : AnalysisData, IPreparableAnalysisData, IUndatedAnalysisData
{
    public override string Name => "Cultural Monuments";

    public override string ReportWebLink => @"https://karte.mantojums.lv";


    protected override string DataFileIdentifier => "cultural-monuments";


    public List<CulturalMonument> Entities { get; private set; } = null!; // only null before prepared
        

    protected override void Download()
    {
        // https://api.mantojums.lv/docs/index.html
        
        // TODO: https://api.mantojums.lv/api/CulturalObjects?group=Monument&Page=4
        // https://api.mantojums.lv/api/CulturalObjects?group=Monument&Page=4&ShouldPage=false&ShouldLimit=false

        throw new NotImplementedException();
        
        WebsiteDownloadHelper.Download(
            "https://api.mantojums.lv/api/CulturalObjects?group=Monument&Page=4", 
            cacheBasePath + DataFileIdentifier + "-" + 1337 + @".json"
        );
    }

    public void Prepare()
    {
        Entities = new List<CulturalMonument>();
        
        string text = File.ReadAllText(cacheBasePath + DataFileIdentifier + "-" + 1337 + @".json");
        
         // todo: actually parse json?
    }
}