namespace Osmalyzer;

public class GTFSTrip
{
    public string Id { get; }
        
    public GTFSRoute Route { get; }
    
    public GTFSService? Service { get; }

    public GTFSShape? Shape { get; }

    
    public IEnumerable<GTFSVisit> Visits => _visits.AsReadOnly();
        
    public IEnumerable<GTFSStop> Stops => _visits.Select(p => p.Stop);


    private readonly List<GTFSVisit> _visits = [ ];


    public GTFSTrip(string id, GTFSRoute route, GTFSService? service, GTFSShape? shape = null)
    {
        Id = id;
        Route = route;
        Service = service;
        Shape = shape;
    }

        
    public void AddPoint(GTFSVisit visit)
    {
        _visits.Add(visit);
    }


    public override string ToString()
    {
        return 
            "Trip #" + Id + " for route #" + Route.Id + " " +
            (Service != null ? "and service #" + Service.Id : "and no service");
    }
}