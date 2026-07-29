namespace Osmalyzer;

public class GTFSVisit
{
    public GTFSTrip? Trip { get; }
        
    public GTFSStop Stop { get; }
    
    public int? PickupType { get; }
    
    public int? DropOffType { get; }


    public GTFSVisit(GTFSTrip? trip, GTFSStop stop, int? pickupType, int? dropOffType)
    {
        Trip = trip;
        Stop = stop;
        PickupType = pickupType;
        DropOffType = dropOffType;
    }
}