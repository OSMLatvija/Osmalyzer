using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class RoadLawAnalysisData : AnalysisData, IPreparableAnalysisData
{
    public override string Name => "Road Law";

    protected override string DataFileIdentifier => "road-law";
        
    public RoadLaw RoadLaw { get; private set; } = null!; // only null during initialization


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://likumi.lv/ta/id/198589-noteikumi-par-valsts-autocelu-un-valsts-autocelu-maroadLawruta-ietverto-pasvaldibam-piederoso-autocelu-posmu-sarakstiem", 
            cacheBasePath + DataFileIdentifier + @".html"
        );
    }

    public void Prepare()
    {
        RoadLaw = new RoadLaw(cacheBasePath + DataFileIdentifier + @".html");
    }
}