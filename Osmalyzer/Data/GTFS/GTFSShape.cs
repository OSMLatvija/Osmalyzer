namespace Osmalyzer;

public class GTFSShape
{
    public string Id { get; }

    
    public IEnumerable<GTFSShapePoint> Points => _points.AsReadOnly();

    public IEnumerable<GTFSTrip> Trips => _trips.AsReadOnly();

    
    private readonly List<GTFSShapePoint> _points = [];

    private readonly List<GTFSTrip> _trips = [];

    
    public GTFSShape(string id)
    {
        Id = id;
    }

    
    public void AddPoint(GTFSShapePoint point)
    {
        _points.Add(point);
    }

    public void AddTrip(GTFSTrip trip)
    {
        _trips.Add(trip);
    }
}