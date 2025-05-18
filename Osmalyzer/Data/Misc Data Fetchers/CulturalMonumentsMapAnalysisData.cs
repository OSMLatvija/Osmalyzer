using FlatGeobuf.NTS;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Osmalyzer;

[UsedImplicitly]
public class CulturalMonumentsMapAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Cultural Monuments";

    public override string ReportWebLink => @"https://karte.mantojums.lv";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "cultural-monuments";


    public List<CulturalMonument> Monuments { get; private set; } = null!; // only null before prepared


    private readonly string[] _variants = { "10", "15", "16", "20" };
    

    protected override void Download()
    {
        // https://karte.mantojums.lv
        // It has MapBox renderer and fetches FGB files from backend

        foreach (string variant in _variants)
        {
            WebsiteDownloadHelper.Download(
                @"https://karte.mantojums.lv/fgb/zoom" + variant + @"-points.fgb",
                Path.Combine(CacheBasePath, DataFileIdentifier + "-" + variant + ".fgb")
            );
        }
    }

    protected override void DoPrepare()
    {
        Monuments = new List<CulturalMonument>();
        
        // Parse the FlatGeobuf FGB files
        
        foreach (string variant in _variants)
        {
            string filePath = Path.Combine(CacheBasePath, DataFileIdentifier + "-" + variant + ".fgb");

            AsyncFeatureEnumerator enumerator = AsyncFeatureEnumerator.Create(File.OpenRead(filePath)).Result;

            //int count = 0;
            
            while (enumerator.MoveNextAsync().Result)
            {
                IFeature feature = enumerator.Current;

                Point centroid = feature.Geometry.Centroid;
                OsmCoord coord = new OsmCoord(centroid.Y, centroid.X);

                List<string> names = feature.Attributes.GetNames().ToList();

                int nameIndex = names.IndexOf("name");
                int monRefIndex = names.IndexOf("national_protection_number");
                // the third one is "id" but it's not the system ID, it's some different ID for map stuff 

                object[] values = feature.Attributes.GetValues();

                string name = CleanName(values[nameIndex].ToString()!);
                string monRefValue = values[monRefIndex].ToString()!;
                int? monRef = null;
                if (monRefValue != "") // there are some with missing id
                    if (int.TryParse(monRefValue, out int value)) // there are some with malformed id
                        monRef = value;

                // There are repeats, so keep each only once
                if (!Monuments.Any(m => m.Name == name && m.ReferenceID == monRef))
                    Monuments.Add(new CulturalMonument(coord, name, monRef, variant));

                //count++;
            }
            
            //Console.WriteLine("Got " + count + " monuments from " + variant + " variant.");
        }
    }

    
    [Pure]
    private static string CleanName(string name)
    {
        name = name.Replace("\r\n", " ");
        name = name.Replace("\n", " ");
        name = name.Replace("\r", " ");
        name = name.Replace("\t", " ");
        name = name.Replace("<br>", " ");
        
        name = Regex.Replace(name, @"\s{2,}", " ");
        
        return name.Trim();
    }
}