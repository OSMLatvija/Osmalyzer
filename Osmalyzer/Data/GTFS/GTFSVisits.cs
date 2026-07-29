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

            int? pickupType = null;
            if (segments.Count > 6)
                pickupType = int.Parse(segments[6]);
            
            int? dropOffType = null;
            if (segments.Count > 7)
                dropOffType = int.Parse(segments[7]);
            
            GTFSVisit newVisit = new GTFSVisit(trip, stop, pickupType, dropOffType);

            _visits.Add(newVisit);

            trip?.AddPoint(newVisit);

            stop.AddPoint(newVisit);
        }
    }
}