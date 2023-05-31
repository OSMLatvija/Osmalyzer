using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Osmalyzer
{
    public class RigasSatiksmeServices
    {
        public IEnumerable<RigasSatiksmeService> Services => _services.AsReadOnly();

        
        private readonly List<RigasSatiksmeService> _services;

        
        public RigasSatiksmeServices(string dataFileName, RigasSatiksmeRoutes routes)
        {
            _services = new List<RigasSatiksmeService>();

            string[] lines = File.ReadAllLines(dataFileName);

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) // header row
                    continue;
            
                string line = lines[i];
                // route_id,service_id,trip_id,service_headsign,direction_id,block_id,shape_id,wheelchair_accessible
                // riga_bus_9,23274,1279,"Abrenes iela",1,169766,riga_bus_9_b-a,

                string[] segments = line.Split(',');

                // route_id - riga_bus_9
                // service_id - 23274
                // trip_id - 1279
                // service_headsign - "Abrenes iela"
                // direction_id - 1
                // block_id - 169766
                // shape_id - riga_bus_9_b-a
                // wheelchair_accessible -

                string serviceId = segments[1];

                if (_services.All(s => s.Id != serviceId)) // this list has trips, not services, so it's repeats
                {
                    RigasSatiksmeService service = new RigasSatiksmeService(serviceId);
                    _services.Add(service);

                    string routeId = segments[0];
                    RigasSatiksmeRoute route = routes.GetRoute(routeId);
                    route.AddService(service);
                }
            }
        }

        
        [Pure]
        public RigasSatiksmeService GetService(string id)
        {
            return _services.First(t => t.Id == id);
        }
    }
}