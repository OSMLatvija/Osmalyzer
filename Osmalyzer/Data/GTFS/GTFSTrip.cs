namespace Osmalyzer;

public class GTFSTrip
{
    public string Id { get; }
        
    public GTFSRoute Route { get; }
    
    public GTFSService? Service { get; }

    public IEnumerable<GTFSPoint> Points => _points.AsReadOnly();
        
    public IEnumerable<GTFSStop> Stops => _points.Select(p => p.Stop);


    private readonly List<GTFSPoint> _points = new List<GTFSPoint>();


    public GTFSTrip(string id, GTFSRoute route, GTFSService? service)
    {
        Id = id;
        Route = route;
        Service = service;
    }

        
    public void AddPoint(GTFSPoint point)
    {
        _points.Add(point);
    }


    public override string ToString()
    {
        return 
            "Trip #" + Id + " for route #" + Route.Id + " " +
            (Service != null ? "and service #" + Service.Id : "and no service");
    }
}