using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using OsmSharp;

namespace Osmalyzer
{
    public class OsmRelation : OsmElement
    {
        public override OsmElementType ElementType => OsmElementType.Relation;

        public override string OsmViewUrl => "https://www.openstreetmap.org/relation/" + Id;

        [PublicAPI]
        public IReadOnlyList<OsmRelationMember> Members => members.AsReadOnly();

        /// <summary>
        ///
        /// This will not contain null/missing elements, even if some are not loaded.
        /// </summary>
        [PublicAPI]
        public IEnumerable<OsmElement> Elements => members.Where(m => m.Element != null).Select(m => m.Element)!;
        
        
        internal readonly List<OsmRelationMember> members;


        internal OsmRelation(OsmGeo rawElement)
            : base(rawElement)
        {
            members = ((Relation)rawElement).Members.Select(m => new OsmRelationMember(this, m.Type, m.Id, m.Role)).ToList();
        }

        
        public OsmPolygon GetOuterWayPolygon()
        {
            List<OsmWay> outerWays = GetOuterWays();

            outerWays = OsmAlgorithms.SortWays(outerWays);

            List<OsmNode> nodes = OsmAlgorithms.CollectNodes(outerWays);

            return new OsmPolygon(nodes.Select(n => (Lat: n.lat, Lon: n.lon)).ToList());
        }

        public List<OsmWay> GetOuterWays()
        {
            List<OsmWay> outerWays = new List<OsmWay>();

            foreach (OsmRelationMember member in Members)
            {
                if (member.Element is OsmWay wayElement && member.Role == "outer")
                {
                    outerWays.Add(wayElement);
                }
            }

            return outerWays;
        }

        public (double lat, double lon) GetAverageElementCoord()
        {
            double averageLat = 0.0;
            double averageLon = 0.0;

            List<OsmElement> elements = Elements.ToList();
            
            foreach (OsmElement element in elements)
            {
                switch (element)
                {
                    case OsmNode osmNode:
                    {
                        averageLat += osmNode.lat / elements.Count;
                        averageLon += osmNode.lon / elements.Count;
                        break;
                    }

                    case OsmWay osmWay:
                    {
                        (double lat, double lon) = osmWay.GetAverageNodeCoord();
                        averageLat += lat / elements.Count;
                        averageLon += lon / elements.Count;
                        break;
                    }

                    case OsmRelation osmRelation:
                    {
                        (double lat, double lon) = osmRelation.GetAverageElementCoord(); // recursion will kill us
                        averageLat += lat / elements.Count;
                        averageLon += lon / elements.Count;
                        break;
                    }

                    default:
                        throw new InvalidOperationException();
                }
            }

            return (averageLat, averageLon);
        }
    }
}