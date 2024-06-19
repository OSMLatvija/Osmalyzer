using System.Collections.Generic;

namespace Osmalyzer;

public abstract class ParcelLockerAnalysisData : AnalysisData, IUndatedAnalysisData, IParcelLockerListProvider
{
    public override bool NeedsPreparation => true;

    
    public abstract IEnumerable<ParcelLocker> ParcelLockers { get; }
    
    public abstract IEnumerable<ParcelPickupPoint>? PickupPoints { get; }
    
    public abstract PickupPointAmenity? PickupPointLocation { get; }
    
    public abstract string? PickupPointLocationName { get; }
}