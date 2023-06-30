using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer
{
    public static class OsmGeoTools
    {
        /// <summary>
        /// In meters.
        /// https://stackoverflow.com/a/51839058
        /// </summary>
        [Pure]
        public static double DistanceBetween(double lat1, double lon1, double lat2, double lon2)
        {
            double d1 = lat1 * (Math.PI / 180.0);
            double num1 = lon1 * (Math.PI / 180.0);
            double d2 = lat2 * (Math.PI / 180.0);
            double num2 = lon2 * (Math.PI / 180.0) - num1;
            double d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }

        [Pure]
        public static (double lat, double lon) GetAverageCoord(IEnumerable<OsmElement> elements)
        {
            double averageLat = 0.0;
            double averageLon = 0.0;

            List<OsmElement> elementList = elements.ToList();
            
            foreach (OsmElement element in elementList)
            {
                (double lat, double lon) = element.GetAverageCoord(); // todo: recursion on circular relations will kill us
                averageLat += lat / elementList.Count;
                averageLon += lon / elementList.Count;
            }

            return (averageLat, averageLon);
        }
    }
}