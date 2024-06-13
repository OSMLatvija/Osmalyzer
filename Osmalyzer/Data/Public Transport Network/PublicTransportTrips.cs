using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Osmalyzer;

public class PublicTransportTrips
{
    public IEnumerable<PublicTransportTrip> Trips => _trips.Values.AsEnumerable();

        
    private readonly Dictionary<string, PublicTransportTrip> _trips;

        
    public PublicTransportTrips(string dataFileName, PublicTransportRoutes routes, PublicTransportServices services)
    {
        _trips = new Dictionary<string, PublicTransportTrip>();

        string[] lines = File.ReadAllLines(dataFileName);

        for (int i = 0; i < lines.Length; i++)
        {
            if (i == 0) // header row
                continue;

            string line = lines[i];
            // route_id,service_id,trip_id,trip_headsign,direction_id,block_id,shape_id,wheelchair_accessible
            // riga_bus_9,23274,1279,"Abrenes iela",1,169766,riga_bus_9_b-a,

            List<string> segments = line.Split(',').Select(s => s.Trim()).ToList();

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

            PublicTransportService? service = services.GetService(serviceId);

            PublicTransportTrip trip = new PublicTransportTrip(tripId, service);
            _trips.TryAdd(trip.Id, trip);

            if (service != null)
            {
                service.AddTrip(trip);

                // Add the service to the route (if it doesn't already know about it)
                // Service may be used for several routes, i.e. different bus numbers do the same service on different days/times or something
                // And vice-verse - add route to service (if not known)

                string routeId = segments[0];
                PublicTransportRoute route = routes.GetRoute(routeId);
                if (route.Services.All(s => s != service))
                    route.AddService(service);

                if (service.Routes.All(r => r != route))
                    service.AddRoute(route);
            }
        }
    }

        
    [Pure]
    public PublicTransportTrip? GetTrip(string id)
    {
        if (_trips.TryGetValue(id, out PublicTransportTrip? s))
            return s;
            
        return null;
    }
}