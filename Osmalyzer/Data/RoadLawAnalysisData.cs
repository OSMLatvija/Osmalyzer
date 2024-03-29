using System.IO;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class RoadLawAnalysisData : AnalysisData, IPreparableAnalysisData, IUndatedAnalysisData
{
    public override string Name => "Road Law";

    public override string ReportWebLink => @"https://likumi.lv/ta/id/198589";


    protected override string DataFileIdentifier => "road-law";
        
    public RoadLaw RoadLaw { get; private set; } = null!; // only null during initialization


    protected override void Download()
    {
        WebsiteBrowsingHelper.DownloadPage( // likumi.lv seems to not like direct download/scraping
            "https://likumi.lv/ta/id/198589", 
            Path.Combine(CacheBasePath, DataFileIdentifier + @".html")
        );
    }

    public void Prepare()
    {
        RoadLaw = new RoadLaw(Path.Combine(CacheBasePath, DataFileIdentifier + @".html"));
    }
}