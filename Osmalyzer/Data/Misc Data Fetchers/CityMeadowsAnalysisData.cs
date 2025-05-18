using SharpKml.Dom;
using SharpKml.Engine;

namespace Osmalyzer;

[UsedImplicitly]
public class CityMeadowsAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "City Meadows";

    public override string ReportWebLink => @"https://ldf.lv/project-tab/ulc-pilsetas-plavas/";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "city-meadows";


    public List<CityMeadow> Meadows { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        const string mapId = "1-YTHGk5SKXKUUaAZ3Wfjf3xiipC31XnY";
        // appears to be a stable ID, so not retrieving from project page

        string kmlUrl = $@"https://www.google.com/maps/d/kml?mid={mapId}&forcekml=1";
        // forcekml to be readable xml kml and not "encoded" kmd
            
        WebsiteDownloadHelper.Download(
            kmlUrl, 
            Path.Combine(CacheBasePath, DataFileIdentifier + @".kml")
        );
    }

    protected override void DoPrepare()
    {
        Meadows = [ ];

        using FileStream fileStream = File.OpenRead(Path.Combine(CacheBasePath, DataFileIdentifier + @".kml"));
            
        KmlFile kmlFile = KmlFile.Load(fileStream);

        IEnumerable<Placemark> placemarks = kmlFile.Root.Flatten().OfType<Placemark>();

        foreach (Placemark placemark in placemarks)
        {
            /* <Placemark>
                <name>Čiekurkalns</name>
                <description><![CDATA[Čiekurkalns    <br>       <br>   FID   10    <br>   Vietas_nos   Čiekurkalns    <br>   Izveides_g   2021    <br>   platiba   0,1308]]></description>
                <styleUrl>#icon-1582-9C27B0-labelson</styleUrl>
                <Point>
                  <coordinates>
                    24.161098,56.985363,0
                  </coordinates>
                </Point>
              </Placemark> */
            // note that the syntax and order in <description> is not guaranteed
            // FID seems to repeat, I think it's if within the same year
            // Vietas_nos seems to always match the name

            if (placemark.Geometry is not Point point)
                continue; // we can only parse points
            
            // todo: we can also load areas to match - but we would need to parse/render them too
            
            string name = CleanupName(placemark.Name);
            
            string? descriptionRaw = placemark.Description?.Text;

            if (descriptionRaw == null)
                continue; // not expecting empty

            Match match = Regex.Match(descriptionRaw, @"Izveides_g\s+(\d{4})");
            
            if (!match.Success)
                continue; // not expecting missing
            
            int startYear = int.Parse(match.Groups[1].ToString());
            
            // todo: platiba ?
                
            Meadows.Add(
                new CityMeadow(
                    new OsmCoord(point.Coordinate.Latitude, point.Coordinate.Longitude),
                    name,
                    startYear
                )
            );
        }
    }

    [Pure]
    private string CleanupName(string name)
    {
        name = name.Trim();

        // Remove underscores
        // "Uzvaras_parks", "Kengaraga_promenade_2" etc.
        name = Regex.Replace(name, @"_", " ");
            
        // Make sure there are proper spaces around any separators
        // "Kr. Valdemāra/ Šarlotes ielas skvērs"
        name = Regex.Replace(name, @"(?<! )([/])", @" $1");
        name = Regex.Replace(name, @"([/])(?! )", @"$1 ");
            
        // Trim numbers at the end
        // "Kengaraga promenade 2"
        name = Regex.Replace(name, @"\d+$", "");
            
        // Other special
            
        if (name == "Strazdupītes pļava l") // they actually used lowercase "l" for "I" as in "1"
            name = "Strazdupītes pļava";
            
        if (name == "Strazdupīte II") // roman numeral
            name = "Strazdupīte";

        if (name == "ĶP Mazjumpravmuiža") // "ĶP" is presumably "Ķengaraga Promenāde"
            name = "Mazjumpravmuiža";

        if (name == "Kengaraga promenade") // misspelled
            name = "Ķengaraga promenāde";

        if (name == "Dreilinkalns") // misspelled
            name = "Dreiliņkalns";

        if (name == "Lielvārdes ielas") // assuming it meant to complete with "pļava"
            name = "Lielvārdes ielas pļava";
            
        // Remove silly abbreviation
        // "LU Botdārzs", "LU Bot dārzs Jūrmalas gatve"
        name = Regex.Replace(name, @"\bBot ?dārzs\b", "Botāniskais dārzs");

        return name.Trim();
    }
}