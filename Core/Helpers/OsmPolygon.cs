using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Osmalyzer
{
    public class OsmPolygon
    {
        private readonly List<OsmCoord> _coords = new List<OsmCoord>();


        public OsmPolygon(List<OsmCoord> coords)
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
                
                _coords.Add(new OsmCoord(lat, lon));
            }
        }

        public bool ContainsElement(OsmElement element, RelationInclusionCheck relationInclusionCheck)
        {
            switch (element)
            {
                case OsmNode node: return ContainsCoord(node.coord);

                case OsmWay way:
                {
                    OsmCoord averageCoord = way.GetAverageCoord();
                    return ContainsCoord(averageCoord);
                }
                
                case OsmRelation relation:
                {
                    switch (relationInclusionCheck)
                    {
                        case RelationInclusionCheck.Fuzzy:
                            int count = 0;
                            int contains = 0;
                    
                            foreach (OsmElement osmElement in relation.Elements)
                            {
                                switch (osmElement)
                                {
                                    case OsmWay way:
                                    {
                                        count += way.nodes.Count;
                                        if (ContainsElement(way, relationInclusionCheck))
                                            contains += way.nodes.Count;
                                        break;
                                    }
                            
                                    case OsmNode node:
                                    {
                                        count++;
                                        if (ContainsElement(node, relationInclusionCheck))
                                            contains++;
                                        break;
                                    }
                                }
                            }
                            
                            return (float)contains / count > 0.3f;
                        
                        default:
                            throw new ArgumentOutOfRangeException(nameof(relationInclusionCheck), relationInclusionCheck, null);
                    }
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(element));
            }
        }

        public bool ContainsCoord(OsmCoord coord)
        {
            bool result = false;

            int b = _coords.Count - 1;

            for (int a = 0; a < _coords.Count; a++)
            {
                if (_coords[a].lon < coord.lon && _coords[b].lon >= coord.lon || _coords[b].lon < coord.lon && _coords[a].lon >= coord.lon)
                    if (_coords[a].lat + (coord.lon - _coords[a].lon) / (_coords[b].lon - _coords[a].lon) * (_coords[b].lat - _coords[a].lat) < coord.lat)
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

            foreach (OsmCoord coord in _coords)
                streamWriter.WriteLine(coord.lon.ToString("E") + " " + coord.lat.ToString("E"));
            
            streamWriter.WriteLine("END");
            streamWriter.WriteLine("END");
            
            streamWriter.Close();
        }


        public enum RelationInclusionCheck
        {
            /// <summary>
            /// Relations are included if a decent portion of their elements are included.
            /// This means relations like roads that barely enter the polygon won't get listed.
            /// </summary>
            Fuzzy
        }
    }
}