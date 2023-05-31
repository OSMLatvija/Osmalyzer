using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Osmalyzer
{
    public class RigasSatiksmeTrips
    {
        public IEnumerable<RigasSatiksmeTrip> Trips => _trips.AsReadOnly();

        
        private readonly List<RigasSatiksmeTrip> _trips;

        
        public RigasSatiksmeTrips(string dataFileName, RigasSatiksmeRoutes routes, RigasSatiksmeServices services)
        {
            _trips = new List<RigasSatiksmeTrip>();

            string[] lines = File.ReadAllLines(dataFileName);

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) // header row
                    continue;
            
                string line = lines[i];
                // route_id,service_id,trip_id,trip_headsign,direction_id,block_id,shape_id,wheelchair_accessible
                // riga_bus_9,23274,1279,"Abrenes iela",1,169766,riga_bus_9_b-a,

                string[] segments = line.Split(',');

                // route_id - riga_bus_9
                // service_id - 23274
                // trip_id - 1279
                // trip_headsign - "Abrenes iela"
                // direction_id - 1
                // block_id - 169766
                // shape_id - riga_bus_9_b-a
                // wheelchair_accessible -

                string tripId = segments[2];
                string serviceId = segments[1];
                RigasSatiksmeService service = services.GetService(serviceId);

                RigasSatiksmeTrip trip = new RigasSatiksmeTrip(tripId, service);

                _trips.Add(trip);

                service.AddTrip(trip);
            }
        }

        
        [Pure]
        public RigasSatiksmeTrip GetTrip(string id)
        {
            return _trips.First(t => t.Id == id);
        }
    }
}