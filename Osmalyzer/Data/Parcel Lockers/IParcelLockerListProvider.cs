using System.Collections.Generic;

namespace Osmalyzer;

public interface IParcelLockerListProvider
{
    IEnumerable<ParcelLocker> ParcelLockers { get; }
    
    IEnumerable<ParcelPickupPoint>? PickupPoints { get; }
    
    PickupPointAmenity? PickupPointLocation { get; }
    
    string? PickupPointLocationName { get; }
}


public enum PickupPointAmenity
{
    GasStation, // LP, DPD
    Kiosk // Itella
} 