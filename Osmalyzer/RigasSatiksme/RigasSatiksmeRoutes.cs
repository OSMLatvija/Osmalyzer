using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Osmalyzer
{
    public class RigasSatiksmeRoutes
    {
        public IEnumerable<RigasSatiksmeRoute> Routes => _routes.Values.AsEnumerable();

        
        private readonly Dictionary<string, RigasSatiksmeRoute> _routes;

        
        public RigasSatiksmeRoutes(string dataFileName)
        {
            string[] lines = File.ReadAllLines(dataFileName);

            _routes = new Dictionary<string, RigasSatiksmeRoute>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) // header row
                    continue;
                
                string line = lines[i];
                // route_id,route_short_name,route_long_name,route_desc,route_type,route_url,route_color,route_text_color,route_sort_order
                // riga_bus_3,"3","Daugavgrīva - Pļavnieki",,3,https://saraksti.rigassatiksme.lv/index.html#riga/bus/3,F4B427,FFFFFF,2000300

                string[] segments = line.Split(',');

                // route_id - riga_bus_3
                // route_short_name - "3"
                // route_long_name - "Daugavgrīva - Pļavnieki"
                // route_desc - 
                // route_type - 3
                // route_url - https://saraksti.rigassatiksme.lv/index.html#riga/bus/3
                // route_color - F4B427
                // route_text_color - FFFFFF
                // route_sort_order - 2000300

                string id = segments[0];
                string name = segments[2].Substring(1, segments[2].Length - 2).Replace("\"\"", "\"");

                RigasSatiksmeRoute route = new RigasSatiksmeRoute(id, name);

                _routes.Add(route.Id, route);
            }
        }

        
        [Pure]
        public RigasSatiksmeRoute GetRoute(string id)
        {
            return _routes[id];
        }
    }
}