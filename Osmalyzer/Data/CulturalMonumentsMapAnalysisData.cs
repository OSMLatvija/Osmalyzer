using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using FlatGeobuf;
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
        
        // Parse the FlatGeobuf FBG file
        
        string filePath = cacheBasePath + DataFileIdentifier + @".fgb";

        AsyncFeatureEnumerator enumerator = AsyncFeatureEnumerator.Create(File.OpenRead(filePath)).Result;

        Console.WriteLine(enumerator.NumFeatures);

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
            
            Monuments.Add(new CulturalMonument(coord, name, monRef));
        }
    }
}