using System;
using System.Collections.Generic;
using System.IO;

namespace Osmalyzer
{
    public class OsmPolygon
    {
        private readonly List<(double lat, double lon)> _coords = new List<(double lat, double lon)>();


        public OsmPolygon(List<(double lat, double lon)> coords)
        {
            _coords = coords;
        }

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

        public bool ContainsElement(OsmElement element)
        {
            switch (element)
            {
                case OsmNode node: return ContainsCoord(node.Lat, node.Lon);
                
                case OsmRelation: return true; // todo: do I care?
                    
                case OsmWay way:
                    (double lat, double lon) averageCoord = way.GetAverageNodeCoord();
                    return ContainsCoord(averageCoord.lat, averageCoord.lon);
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(element));
            }
        }

        public bool ContainsCoord(double lat, double lon)
        {
            bool result = false;

            int b = _coords.Count - 1;

            for (int a = 0; a < _coords.Count; a++)
            {
                if (_coords[a].lon < lon && _coords[b].lon >= lon || _coords[b].lon < lon && _coords[a].lon >= lon)
                    if (_coords[a].lat + (lon - _coords[a].lon) / (_coords[b].lon - _coords[a].lon) * (_coords[b].lat - _coords[a].lat) < lat)
                        result = !result;

                b = a;
            }

            return result;
        }

        public void SaveToFile(string fileName)
        {
            using StreamWriter streamWriter = File.CreateText(fileName);

            streamWriter.WriteLine("none");
            streamWriter.WriteLine("1");

            foreach ((double lat, double lon) in _coords)
                streamWriter.WriteLine(lon.ToString("E") + " " + lat.ToString("E"));
            
            streamWriter.WriteLine("END");
            streamWriter.WriteLine("END");
            
            streamWriter.Close();
        }
    }
}