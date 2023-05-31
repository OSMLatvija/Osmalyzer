using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Osmalyzer
{
    public class RigasSatiksmeStops
    {
        public IEnumerable<RigasSatiksmeStop> Stops => _stops.Values.AsEnumerable();

        
        private readonly Dictionary<string, RigasSatiksmeStop> _stops;

        
        public RigasSatiksmeStops(string dataFileName)
        {
            string[] lines = File.ReadAllLines(dataFileName);

            _stops = new Dictionary<string, RigasSatiksmeStop>();

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

                RigasSatiksmeStop stop = new RigasSatiksmeStop(id, name, lat, lon);

                _stops.Add(stop.Id, stop);
            }
        }

        
        [Pure]
        public RigasSatiksmeStop GetStop(string id)
        {
            return _stops[id];
        }
    }
}