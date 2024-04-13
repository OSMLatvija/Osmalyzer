using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using SharpKml.Dom;
using SharpKml.Engine;

namespace Osmalyzer;

[UsedImplicitly]
public class GlikaOzoliAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Glika Ozoli";

    public override string ReportWebLink => @"https://www.lelb.lv/lv/?ct=glika_ozoli";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "glika-ozoli";


    public List<GlikaOak> Oaks { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        string infoPageText = WebsiteDownloadHelper.Read("https://www.lelb.lv/lv/?ct=glika_ozoli", true);
            
        Match mapMatch = Regex.Match(infoPageText, @"<iframe src=""https://www\.google\.com/maps/d/embed\?mid=([a-zA-Z0-9_]+)&");
        // https://www.google.com/maps/d/viewer?mid=1wRS7q3l_ESgCVKjHm1lO_dW0o3rSJYU

        string mapId = mapMatch.Groups[1].ToString();

        string kmlUrl = $@"https://www.google.com/maps/d/kml?mid={mapId}&forcekml=1";
        // forcekml to be readable xml kml and not "encoded" kmd
        // https://www.google.com/maps/d/kml?mid=1wRS7q3l_ESgCVKjHm1lO_dW0o3rSJYU&forcekml=1
            
        WebsiteDownloadHelper.Download(
            kmlUrl, 
            Path.Combine(CacheBasePath, DataFileIdentifier + @".kml")
        );
    }

    protected override void DoPrepare()
    {
        Oaks = new List<GlikaOak>();

        using FileStream fileStream = File.OpenRead(Path.Combine(CacheBasePath, DataFileIdentifier + @".kml"));
            
        KmlFile kmlFile = KmlFile.Load(fileStream);

        IEnumerable<Placemark> placemarks = kmlFile.Root.Flatten().OfType<Placemark>();

        List<string> names = new List<string>();
        List<string> repeats = new List<string>();
            
        foreach (Placemark placemark in placemarks)
        {
            if (placemark.Geometry is Point point)
            {
                string name = placemark.Name;
                    
                if (name.ToLower().Contains("vides objekts"))
                    continue;

                string startDate = GetStartDate(placemark);
                    
                Oaks.Add(
                    new GlikaOak(
                        new OsmCoord(point.Coordinate.Latitude, point.Coordinate.Longitude),
                        name,
                        placemark.Description?.Text,
                        startDate
                    )
                );
                    
                if (!names.Contains(name))
                    names.Add(name);
                else if (!repeats.Contains(name))
                    repeats.Add(name);
            }
        }

        // Disambiguate same-named trees, probably planted at the same time (in the same place)
            
        foreach (string repeat in repeats)
        {
            int id = 1;
                
            foreach (GlikaOak oak in Oaks)
            {
                if (oak.Name == repeat)
                {
                    oak.Id = id;
                    id++;
                }
            }
        }
    }

    [Pure]
    private static string GetStartDate(Placemark placemark)
    {
        // GLIKA OZOLU stādīšanas vietas.
        // Dzeltens – rudens 2022
        // Zaļš – pavasaris 2022
        // Zils – vides objekti
        // Gaiši zaļš - pavasaris 2023
            
        // Yellow - <styleUrl>#icon-1886-FBC02D</styleUrl> 
        // Green - <styleUrl>#icon-1886-006064</styleUrl>
        // Light green - <styleUrl>#icon-1886-7CB342</styleUrl>

        string colorString = Regex.Match(placemark.StyleUrl.OriginalString, @"[0-9A-F]{6}").Groups[0].ToString();

        // TODO: not season, but months - I don't know exactly what they are though, data uses seasons
            
        return colorString switch
        {
            "FBC02D" => "autumn 2022",
            "006064" => "spring 2022",
            "7CB342" => "spring 2023",
            "C2185B" => "summer+ 2023?",
            _        => throw new NotImplementedException("Unknown color code")
        };
    }
}