using System.Web;
using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class StatePoliceAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "StatePolice";

    public override string ReportWebLink => @"https://www.vp.gov.lv/lv/valsts-policijas-apkalpojamas-teritorijas-un-iecirknu-pienemsanas-laiki#valsts-policijas-atrasanas-vietas";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "state-police";

    private string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");


    public List<StatePoliceData> Offices { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://geolatvija.lv/api/v1/user-embeds/71648f8f-22b7-41cc-b06e-86028ec6938b/uuid", 
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        Offices = [ ];

        string source = File.ReadAllText(DataFileName);
        dynamic outerContent = JsonConvert.DeserializeObject(source)!;
        string innerSource = (string)outerContent.data;
        dynamic innerContent = JsonConvert.DeserializeObject(innerSource)!;
        // {
        //     "uuid":"9182bc41-ee7c-42a6-b511-2ac83a7d0c85",
        //     "coord":[
        //         448684.228531708,
        //         313777.3467708682
        //     ],
        //     "color":"#518B33",
        //     "description":"Valsts policijas Zemgales reģiona pārvaldes Rietumzemgales iecirknis Tukumā"
        // }, 
        foreach (dynamic item in innerContent.markers)
        {
            double northing = item.coord[0];
            double easting = item.coord[1];
            
            (double lat, double lon) = CoordConversion.LKS92ToWGS84(northing, easting);
            
            Offices.Add(
                new StatePoliceData(
                    (string)item.description,
                    new OsmCoord(lat, lon)
                )
            );
        }
    }
}