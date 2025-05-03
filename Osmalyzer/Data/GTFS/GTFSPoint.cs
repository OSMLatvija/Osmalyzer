namespace Osmalyzer;

public class GTFSPoint
{
    public GTFSTrip? Trip { get; }
        
    public GTFSStop Stop { get; }


    public GTFSPoint(GTFSTrip? trip, GTFSStop stop)
    {
        Trip = trip;
        Stop = stop;
    }
}