using System;
using System.Collections.Generic;
using System.IO;

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

        public bool ContainsElement(OsmElement element)
        {
            switch (element)
            {
                case OsmNode node: return ContainsNode(node);
                
                case OsmRelation: return true;
                    
                case OsmWay way: return ContainsWayMidpoint(way);
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(element));
            }
        }

        public bool ContainsNode(OsmNode node)
        {
            return ContainsCoord(node.Lat, node.Lon);
        }

        public bool ContainsWayMidpoint(OsmWay way)
        {
            // TODO: I have no refs
            // TODO: I have no refs
            // TODO: I have no refs
            // way.Nodes.Select(n => n.Lat).Average();

            return true;
        }

        public bool ContainsCoord(double lat, double lon)
        {
            // TODO: ACTUAL
            // TODO: ACTUAL
            // TODO: ACTUAL

            return true;
        }
    }
}