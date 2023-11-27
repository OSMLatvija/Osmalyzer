using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using FlatGeobuf.NTS;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Osmalyzer;

[UsedImplicitly]
public class CulturalMonumentsMapAnalysisData : AnalysisData, IPreparableAnalysisData, IUndatedAnalysisData
{
    public override string Name => "Cultural Monuments";

    public override string ReportWebLink => @"https://karte.mantojums.lv";


    protected override string DataFileIdentifier => "cultural-monuments";


    public List<CulturalMonument> Monuments { get; private set; } = null!; // only null before prepared


    private readonly string[] _variants = { "10", "15", "16", "20" };
    

    protected override void Download()
    {
        if (!Directory.Exists(cacheBasePath + DataFileIdentifier))
            Directory.CreateDirectory(cacheBasePath + DataFileIdentifier);
        
        // https://karte.mantojums.lv
        // It has MapBox renderer and fetches FBG files from backend

        foreach (string variant in _variants)
        {
            WebsiteDownloadHelper.Download(
                @"https://karte.mantojums.lv/fgb/zoom" + variant + @"-points.fgb",
                cacheBasePath + DataFileIdentifier + "-" + variant + ".fgb"
            );
        }
    }

    public void Prepare()
    {
        Monuments = new List<CulturalMonument>();
        
        // Parse the FlatGeobuf FBG files
        
        foreach (string variant in _variants)
        {
            string filePath = cacheBasePath + DataFileIdentifier + "-" + variant + ".fgb";

            AsyncFeatureEnumerator enumerator = AsyncFeatureEnumerator.Create(File.OpenRead(filePath)).Result;

            while (enumerator.MoveNextAsync().Result)
            {
                IFeature feature = enumerator.Current;

                Point centroid = feature.Geometry.Centroid;
                OsmCoord coord = new OsmCoord(centroid.Y, centroid.X);

                List<string> names = feature.Attributes.GetNames().ToList();

                int nameIndex = names.IndexOf("name");
                int monRefIndex = names.IndexOf("national_protection_number");

                object[] values = feature.Attributes.GetValues();

                string name = values[nameIndex].ToString()!.Trim(); // there are some with newlines in name
                string monRefValue = values[monRefIndex].ToString()!;
                int? monRef = monRefValue != "" ? int.Parse(monRefValue) : null; // there are some with missing id

                // There are repeats, so keep each only once
                if (!Monuments.Any(m => m.Name == name && m.ReferenceID == monRef))
                    Monuments.Add(new CulturalMonument(coord, name, monRef));
            }
        }
    }
}