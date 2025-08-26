using System.Text.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class LVMPicnicSiteAnalysisData : AnalysisData
{
    public override string Name => "LVM Picnic Sites";

    public override string ReportWebLink => @"https://www.mammadaba.lv/karte";

    protected override string DataFileIdentifier => "lvm-picnic-sites";

    public override bool NeedsPreparation => true;

    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");

    public List<LVMPicnicSiteData> PicnicSites = null!;

    protected override void Download()
    {
        // https://lvmkartes.lvm.lv
        // layer 9 = picnic sites
        WebsiteDownloadHelper.Download(
            @"https://lvmkartes.lvm.lv/mammadaba/proxy/mamma1b719989bb6144a28e0d804259abb01d/MDvMapInfraWGS_V2/FeatureServer/9/query?f=json&returnGeometry=true&where=1%3D1&outFields=*&outSR=4326",
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        string source = File.ReadAllText(DataFileName);

        using (JsonDocument doc = JsonDocument.Parse(source))
        {
            JsonElement root = doc.RootElement;

            PicnicSites = new List<LVMPicnicSiteData>();

            foreach (JsonElement place in root.GetProperty("features").EnumerateArray())
            {
                JsonElement attributes = place.GetProperty("attributes");
                JsonElement geometry = place.GetProperty("geometry");

                string name = attributes.GetProperty("OBJECTNAME").GetString();

                double lat = geometry.GetProperty("y").GetDouble();
                double lon = geometry.GetProperty("x").GetDouble();

                PicnicSites.Add(
                    new LVMPicnicSiteData(
                        name,
                        new OsmCoord(lat, lon)
                    )
                );
            }
        }
    }
}