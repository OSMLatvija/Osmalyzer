namespace Osmalyzer;

public class GTFSVisit
{
    public GTFSTrip? Trip { get; }
        
    public GTFSStop Stop { get; }
    
    public GTFSPickupType PickupType { get; }
    
    public GTFSDropoffType DropOffType { get; }


    public GTFSVisit(GTFSTrip? trip, GTFSStop stop, GTFSPickupType pickupType, GTFSDropoffType dropOffType)
    {
        Trip = trip;
        Stop = stop;
        PickupType = pickupType;
        DropOffType = dropOffType;
    }
}