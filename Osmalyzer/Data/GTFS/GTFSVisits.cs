namespace Osmalyzer;

/// <summary>
/// "Times that a vehicle arrives at and departs from stops for each trip."
/// </summary>
public class GTFSVisits
{
    public IEnumerable<GTFSVisit> Visits => _visits.AsReadOnly();

        
    private readonly List<GTFSVisit> _visits;

        
    public GTFSVisits(string dataFileName, GTFSStops stops, GTFSTrips trips)
    {
        _visits = [ ];
            
        string[] lines = File.ReadAllLines(dataFileName);

        for (int i = 0; i < lines.Length; i++)
        {
            if (i == 0) // header row
                continue;

            // https://gtfs.org/documentation/schedule/reference/#stopstxt
            
            string line = lines[i];
            // trip_id,arrival_time,departure_time,stop_id,stop_sequence,pickup_type,drop_off_type
            // 3853,06:44:00,06:44:00,1336,7,0,3,3

            List<string> segments = line.Split(',').Select(s => s.Trim()).ToList();

            // trip_id - 3853
            // arrival_time - 06:44:00
            // departure_time - 06:44:00
            // stop_id - 1336
            // stop_sequence - 7
            // ?????? - 0 --- RS data has 7 headers but 8 values...
            // pickup_type - 3
            // drop_off_type - 3

            string tripId = segments[0];
            GTFSTrip? trip = trips.GetTrip(tripId);

            string stopId = segments[3];
            GTFSStop? stop = stops.GetStop(stopId);
            if (stop == null) 
                continue; // broken data?

            GTFSPickupType pickupType = GTFSPickupType.Unknown;
            if (segments.Count > 6)
                pickupType = GetPickupTypeFromValue(segments[6]);
            
            GTFSDropoffType dropOffType = GTFSDropoffType.Unknown;
            if (segments.Count > 7)
                dropOffType = GetDropoffTypeFromValue(segments[7]);
            
            GTFSVisit newVisit = new GTFSVisit(trip, stop, pickupType, dropOffType);

            _visits.Add(newVisit);

            trip?.AddPoint(newVisit);

            stop.AddPoint(newVisit);
        }
    }

    
    [Pure]
    private static GTFSPickupType GetPickupTypeFromValue(string value)
    {
        return value switch
        {
            "0" => GTFSPickupType.RegularlyScheduledPickup,
            "1" => GTFSPickupType.NoPickupAvailable,
            "2" => GTFSPickupType.MustPhoneAgencyToArrangePickup,
            "3" => GTFSPickupType.MustCoordinateWithDriverToArrangePickup,
            _   => GTFSPickupType.Invalid
        };
    }
    
    [Pure]
    private static GTFSDropoffType GetDropoffTypeFromValue(string value)
    {
        return value switch
        {
            "0" => GTFSDropoffType.RegularlyScheduledPickup,
            "1" => GTFSDropoffType.NoPickupAvailable,
            "2" => GTFSDropoffType.MustPhoneAgencyToArrangePickup,
            "3" => GTFSDropoffType.MustCoordinateWithDriverToArrangePickup,
            _   => GTFSDropoffType.Invalid
        };
    }
}


public enum GTFSPickupType
{
    Invalid,
    Unknown,
    Mixed,
    RegularlyScheduledPickup,
    NoPickupAvailable,
    MustPhoneAgencyToArrangePickup,
    MustCoordinateWithDriverToArrangePickup
}

public enum GTFSDropoffType
{
    Invalid,
    Unknown,
    Mixed,
    RegularlyScheduledPickup,
    NoPickupAvailable,
    MustPhoneAgencyToArrangePickup,
    MustCoordinateWithDriverToArrangePickup
}