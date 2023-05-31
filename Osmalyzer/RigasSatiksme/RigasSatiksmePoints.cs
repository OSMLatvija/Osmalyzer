using System.Collections.Generic;
using System.IO;

namespace Osmalyzer
{
    public class RigasSatiksmePoints
    {
        public IEnumerable<RigasSatiksmePoint> Points => _points.AsReadOnly();

        
        private readonly List<RigasSatiksmePoint> _points;

        
        public RigasSatiksmePoints(string dataFileName, RigasSatiksmeStops stops, RigasSatiksmeTrips trips)
        {
            _points = new List<RigasSatiksmePoint>();
            
            string[] lines = File.ReadAllLines(dataFileName);

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) // header row
                    continue;

                string line = lines[i];
                // trip_id,arrival_time,departure_time,stop_id,stop_sequence,pickup_type,drop_off_type
                // 2961,21:53:00,21:53:00,5003,13,0,0

                string[] segments = line.Split(',');

                // trip_id - 2961
                // arrival_time - 21:53:00
                // departure_time - 21:53:00
                // stop_id - 5003
                // stop_sequence - 13
                // pickup_type - 0
                // drop_off_type - 0

                string tripId = segments[0];
                string stopId = segments[3];
                RigasSatiksmeStop stop = stops.GetStop(stopId);
                RigasSatiksmeTrip trip = trips.GetTrip(tripId);

                RigasSatiksmePoint newPoint = new RigasSatiksmePoint(trip, stop);

                _points.Add(newPoint);

                trip.AddPoint(newPoint);
            }
        }
    }
}