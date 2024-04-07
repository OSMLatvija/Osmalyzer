using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public void Prepare()
    {
        Monuments = new List<CulturalMonument>();
        
        // Parse the FlatGeobuf FGB files
        
        foreach (string variant in _variants)
        {
            string filePath = Path.Combine(CacheBasePath, DataFileIdentifier + "-" + variant + ".fgb");

            AsyncFeatureEnumerator enumerator = AsyncFeatureEnumerator.Create(File.OpenRead(filePath)).Result;

            try // "cultural-monuments-16.fgb" fails to read past a certain point
            {
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

                    string name = values[nameIndex].ToString()!.Trim(); // there are some with newlines in name
                    string monRefValue = values[monRefIndex].ToString()!;
                    int? monRef = monRefValue != "" ? int.Parse(monRefValue) : null; // there are some with missing id

                    // There are repeats, so keep each only once
                    if (!Monuments.Any(m => m.Name == name && m.ReferenceID == monRef))
                        Monuments.Add(new CulturalMonument(coord, name, monRef, variant));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to parse FBG data file variant " + variant + " " + filePath);
                Console.WriteLine(e.Message);
            }
        }
    }
}