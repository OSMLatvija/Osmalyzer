using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Osmalyzer;

public class PublicTransportPoints
{
    public IEnumerable<PublicTransportPoint> Points => _points.AsReadOnly();

        
    private readonly List<PublicTransportPoint> _points;

        
    public PublicTransportPoints(string dataFileName, PublicTransportStops stops, PublicTransportTrips trips)
    {
        _points = new List<PublicTransportPoint>();
            
        string[] lines = File.ReadAllLines(dataFileName);

        for (int i = 0; i < lines.Length; i++)
        {
            if (i == 0) // header row
                continue;

            string line = lines[i];
            // trip_id,arrival_time,departure_time,stop_id,stop_sequence,pickup_type,drop_off_type
            // 2961,21:53:00,21:53:00,5003,13,0,0

            List<string> segments = line.Split(',').Select(s => s.Trim()).ToList();

            // trip_id - 2961
            // arrival_time - 21:53:00
            // departure_time - 21:53:00
            // stop_id - 5003
            // stop_sequence - 13
            // pickup_type - 0
            // drop_off_type - 0

            string tripId = segments[0];
            PublicTransportTrip? trip = trips.GetTrip(tripId);

            string stopId = segments[3];
            PublicTransportStop stop = stops.GetStop(stopId);

            PublicTransportPoint newPoint = new PublicTransportPoint(trip, stop);

            _points.Add(newPoint);

            if (trip != null)
                trip.AddPoint(newPoint);
        }
    }
}