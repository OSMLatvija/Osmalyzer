using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class RoadLawAnalysisData : AnalysisData, IPreparableAnalysisData, IUndatedAnalysisData
{
    public override string Name => "Road Law";

    protected override string DataFileIdentifier => "road-law";
        
    public RoadLaw RoadLaw { get; private set; } = null!; // only null during initialization


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://likumi.lv/ta/id/198589", 
            cacheBasePath + DataFileIdentifier + @".html"
        );
    }

    public void Prepare()
    {
        RoadLaw = new RoadLaw(cacheBasePath + DataFileIdentifier + @".html");
    }
}