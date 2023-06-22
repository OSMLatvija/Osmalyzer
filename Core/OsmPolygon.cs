using System;
using System.Collections.Generic;
using System.IO;
using OsmSharp;

namespace Osmalyzer
{
    public class OsmPolygon
    {
        private readonly List<(double lat, double lon)> _coords = new List<(double lat, double lon)>();
        
        
        public OsmPolygon(string polyFileName)
        {
            // Note: complete assumption about the file structure
            
            // none
            // 1
            //    2.659394E+01   5.566109E+01
            //    2.637334E+01   5.569487E+01
            //    ..
            //    2.659394E+01   5.566109E+01
            // END
            // END
            //
            
            string[] lines = File.ReadAllLines(polyFileName);

            for (int i = 2; i < lines.Length - 2; i++) // first and last 2 lines ignored
            {
                string[] coords = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);

                double lat = double.Parse(coords[1]);
                double lon = double.Parse(coords[0]);
                
                _coords.Add((lat, lon));
            }
        }

        public bool ContainsElement(OsmGeo element)
        {
            return true;
        }
    }
}