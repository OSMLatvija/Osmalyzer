using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class CulturalMonumentsMapAnalysisData : AnalysisData, IPreparableAnalysisData, IUndatedAnalysisData
{
    public override string Name => "Cultural Monuments";

    public override string ReportWebLink => @"https://karte.mantojums.lv";


    protected override string DataFileIdentifier => "cultural-monuments";


    public List<CulturalMonument> Monuments { get; private set; } = null!; // only null before prepared
        

    protected override void Download()
    {
        if (!Directory.Exists(cacheBasePath + DataFileIdentifier))
            Directory.CreateDirectory(cacheBasePath + DataFileIdentifier);
        
        // https://karte.mantojums.lv
        // It has MapBox renderer and fetches FBG files from backend

        WebsiteDownloadHelper.Download(
            @"https://karte.mantojums.lv/fgb/zoom16-points.fgb", 
            cacheBasePath + DataFileIdentifier + @".fgb"
        );
    }

    public void Prepare()
    {
        Monuments = new List<CulturalMonument>();
        
        string content = File.ReadAllText(cacheBasePath + DataFileIdentifier + @".fgb");

        // TODO
    }
}