﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using SharpKml.Dom;
using SharpKml.Engine;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class GikaOzoliAnalysisData : AnalysisData, IPreparableAnalysisData
    {
        public override string Name => "Glika Ozoli";

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
                cacheBasePath + DataFileIdentifier + @".kml"
            );
        }

        public void Prepare()
        {
            Oaks = new List<GlikaOak>();

            using FileStream fileStream = File.OpenRead(cacheBasePath + DataFileIdentifier + @".kml");
            
            KmlFile kmlFile = KmlFile.Load(fileStream);

            IEnumerable<Placemark> placemarks = kmlFile.Root.Flatten().OfType<Placemark>();

            foreach (Placemark placemark in placemarks)
            {
                if (placemark.Geometry is Point point)
                {
                    if (placemark.Name.ToLower().Contains("vides objekts"))
                        continue;
                    
                    Oaks.Add(
                        new GlikaOak(
                            new OsmCoord(point.Coordinate.Latitude, point.Coordinate.Longitude),
                            placemark.Name,
                            placemark.Description?.Text
                        )
                    );
                }
            }
        }
    }
}