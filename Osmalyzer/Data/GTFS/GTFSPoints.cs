﻿namespace Osmalyzer;

/// <summary>
/// "Times that a vehicle arrives at and departs from stops for each trip."
/// </summary>
public class GTFSPoints
{
    public IEnumerable<GTFSPoint> Points => _points.AsReadOnly();

        
    private readonly List<GTFSPoint> _points;

        
    public GTFSPoints(string dataFileName, GTFSStops stops, GTFSTrips trips)
    {
        _points = [ ];
            
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
            GTFSTrip? trip = trips.GetTrip(tripId);

            string stopId = segments[3];
            GTFSStop? stop = stops.GetStop(stopId);
            if (stop == null) 
                continue; // broken data?

            GTFSPoint newPoint = new GTFSPoint(trip, stop);

            _points.Add(newPoint);

            if (trip != null)
                trip.AddPoint(newPoint);
        }
    }
}