namespace Osmalyzer;

public class PublicTransportPoint
{
    public PublicTransportTrip? Trip { get; }
        
    public PublicTransportStop Stop { get; }


    public PublicTransportPoint(PublicTransportTrip? trip, PublicTransportStop stop)
    {
        Trip = trip;
        Stop = stop;
    }
}