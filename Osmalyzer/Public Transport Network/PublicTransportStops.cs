﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer
{
    public class PublicTransportStops
    {
        public IEnumerable<PublicTransportStop> Stops => _stops.Values.AsEnumerable();

        
        private readonly Dictionary<string, PublicTransportStop> _stops;

        
        public PublicTransportStops(string dataFileName)
        {
            string[] lines = File.ReadAllLines(dataFileName);

            _stops = new Dictionary<string, PublicTransportStop>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) // header row
                    continue;
                
                string line = lines[i];
                // stop_id,stop_code,stop_name,stop_desc,stop_lat,stop_lon,stop_url,location_type,parent_station
                // 0470,,"Tallinas iela",,56.95896,24.14143,https://saraksti.rigassatiksme.lv,,

                string[] segments = line.Split(',');

                // stop_id - 0470
                // top_code - 
                // stop_name - "Tallinas iela"
                // stop_desc - 
                // stop_lat - 56.95896
                // stop_lon - 24.14143
                // stop_url - https://saraksti.rigassatiksme.lv
                // location_type - 
                // parent_station - 

                string id = segments[0];
                string name = segments[2].Substring(1, segments[2].Length - 2).Replace("\"\"", "\"");
                double lat = double.Parse(segments[4]);
                double lon = double.Parse(segments[5]);

                PublicTransportStop stop = new PublicTransportStop(id, name, lat, lon);

                if (!_stops.ContainsKey(stop.Id))
                    _stops.Add(stop.Id, stop);
                // Latvijas Autobuss has duplicates, e.g.
                // 7123k,,"Majoru stacija",,56.97155,23.79636,https://www.marsruti.lv/jurmala/index.html#stop/7123k,,
                // 7123k,,"Majori",,56.97155,23.79636,https://www.marsruti.lv/jurmala/index.html#stop/7123k,,
                // (second has paired 7123l,,"Majori",,56.97149,23.79807,https://www.marsruti.lv/jurmala/index.html#stop/7123l,,)
                // todo: report these as problems? only if coord different? store both names?
            }
        }

        
        [Pure]
        public PublicTransportStop GetStop(string id)
        {
            return _stops[id];
        }
    }
}