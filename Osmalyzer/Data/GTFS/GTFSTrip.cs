namespace Osmalyzer;

public class GTFSTrip
{
    public string Id { get; }
        
    public GTFSService? Service { get; }

    public IEnumerable<GTFSPoint> Points => _points.AsReadOnly();
        
    public IEnumerable<GTFSStop> Stops => _points.Select(p => p.Stop);


    private readonly List<GTFSPoint> _points = new List<GTFSPoint>();


    public GTFSTrip(string id, GTFSService? service)
    {
        Id = id;
        Service = service;
    }

        
    public void AddPoint(GTFSPoint point)
    {
        _points.Add(point);
    }


    public override string ToString()
    {
        return "Trip #" + Id + " for " + (Service != null ? "service #" + Service.Id : "no service");
    }
}