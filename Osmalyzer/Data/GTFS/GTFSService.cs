using System.Collections.Generic;

namespace Osmalyzer;

public class GTFSService
{
    public string Id { get; }

    public IEnumerable<GTFSTrip> Trips => _trips.AsReadOnly();
        
    /// <summary>
    /// Note that multiple routes may run the same service, although likely with different points/stops.
    /// </summary>
    public IEnumerable<GTFSRoute> Routes => _routes.AsReadOnly();


    private readonly List<GTFSTrip> _trips = new List<GTFSTrip>();
        
    private readonly List<GTFSRoute> _routes = new List<GTFSRoute>();


    public GTFSService(string id)
    {
        Id = id;
    }

        
    public void AddTrip(GTFSTrip trip)
    {
        _trips.Add(trip);
    }
        
    public void AddRoute(GTFSRoute route)
    {
        _routes.Add(route);
    }
}