using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Osmalyzer;

/// <summary>
/// "Trips for each route. A trip is a sequence of two or more stops that occur during a specific time period."
/// </summary>
public class GTFSTrips
{
    public IEnumerable<GTFSTrip> Trips => _trips.Values.AsEnumerable();

        
    private readonly Dictionary<string, GTFSTrip> _trips;

        
    public GTFSTrips(string dataFileName, GTFSRoutes routes, GTFSServices services)
    {
        _trips = new Dictionary<string, GTFSTrip>();

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

            GTFSService? service = services.GetService(serviceId);

            GTFSTrip trip = new GTFSTrip(tripId, service);
            _trips.TryAdd(trip.Id, trip);

            if (service != null)
            {
                service.AddTrip(trip);

                // Add the service to the route (if it doesn't already know about it)
                // Service may be used for several routes, i.e. different bus numbers do the same service on different days/times or something
                // And vice-verse - add route to service (if not known)

                string routeId = segments[0];
                GTFSRoute route = routes.GetRoute(routeId);
                if (route.Services.All(s => s != service))
                    route.AddService(service);

                if (service.Routes.All(r => r != route))
                    service.AddRoute(route);
            }
        }
    }

        
    [Pure]
    public GTFSTrip? GetTrip(string id)
    {
        if (_trips.TryGetValue(id, out GTFSTrip? s))
            return s;
            
        return null;
    }
}