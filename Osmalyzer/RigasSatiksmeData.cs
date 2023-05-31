using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Osmalyzer
{
    /// <summary>
    ///
    /// https://data.gov.lv/dati/lv/dataset/marsrutu-saraksti-rigas-satiksme-sabiedriskajam-transportam
    /// </summary>
    public class RigasSatiksmeData
    {
        public RigasSatiksmeStops Stops { get; }
        
        public RigasSatiksmeRoutes Routes { get; }
        
        public RigasSatiksmeTrips Trips { get; }


        public RigasSatiksmeData(string dataFolder)
        {
            Stops = new RigasSatiksmeStops(Path.Combine(dataFolder, "stops.txt"));
            Routes = new RigasSatiksmeRoutes(Path.Combine(dataFolder, "routes.txt"));
            Trips = new RigasSatiksmeTrips(Path.Combine(dataFolder, "trips.txt"), Path.Combine(dataFolder, "stop_times.txt"), Stops, Routes);
        }
    }
    
    public class RigasSatiksmeStops
    {
        public IEnumerable<RigasSatiksmeStop> Stops => _stops.AsReadOnly();

        
        private readonly List<RigasSatiksmeStop> _stops;

        
        public RigasSatiksmeStops(string dataFileName)
        {
            string[] lines = File.ReadAllLines(dataFileName);

            _stops = new List<RigasSatiksmeStop>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) // header row
                    continue;
                
                string line = lines[i];
                // stop_id,stop_code,stop_name,stop_desc,stop_lat,stop_lon,stop_url,location_type,parent_station
                // 0470,,"Tallinas iela",,56.95896,24.14143,https://saraksti.rigassatiksme.lv,,

                string[] segments = line.Split(',');

                // stop_id - 0470
                // top_code - 
                // stop_name - "Tallinas iela"
                // stop_desc - 
                // stop_lat - 56.95896
                // stop_lon - 24.14143
                // stop_url - https://saraksti.rigassatiksme.lv
                // location_type - 
                // parent_station - 

                string id = segments[0];
                string name = segments[2].Substring(1, segments[2].Length - 2).Replace("\"\"", "\"");
                double lat = double.Parse(segments[4]);
                double lon = double.Parse(segments[5]);

                RigasSatiksmeStop stop = new RigasSatiksmeStop(id, name, lat, lon);

                _stops.Add(stop);
            }
        }

        
        [Pure]
        public RigasSatiksmeStop GetStop(string stopId)
        {
            return _stops.First(s => s.Id == stopId);
        }
    }
    
    public class RigasSatiksmeStop
    {
        public string Id { get; }

        public string Name { get; }
        
        public double Lat { get; }
        
        public double Lon { get; }


        public RigasSatiksmeStop(string id, string name, double lat, double lon)
        {
            Id = id;
            Name = name;
            Lat = lat;
            Lon = lon;
        }
    }
    
    public class RigasSatiksmeRoutes
    {
        public IEnumerable<RigasSatiksmeRoute> Routes => _routes.AsReadOnly();

        
        private readonly List<RigasSatiksmeRoute> _routes;

        
        public RigasSatiksmeRoutes(string dataFileName)
        {
            string[] lines = File.ReadAllLines(dataFileName);

            _routes = new List<RigasSatiksmeRoute>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0) // header row
                    continue;
                
                string line = lines[i];
                // route_id,route_short_name,route_long_name,route_desc,route_type,route_url,route_color,route_text_color,route_sort_order
                // riga_bus_3,"3","Daugavgrīva - Pļavnieki",,3,https://saraksti.rigassatiksme.lv/index.html#riga/bus/3,F4B427,FFFFFF,2000300

                string[] segments = line.Split(',');

                // route_id - riga_bus_3
                // route_short_name - "3"
                // route_long_name - "Daugavgrīva - Pļavnieki"
                // route_desc - 
                // route_type - 3
                // route_url - https://saraksti.rigassatiksme.lv/index.html#riga/bus/3
                // route_color - F4B427
                // route_text_color - FFFFFF
                // route_sort_order - 2000300

                string id = segments[0];
                string name = segments[2].Substring(1, segments[2].Length - 2).Replace("\"\"", "\"");

                RigasSatiksmeRoute route = new RigasSatiksmeRoute(id, name);

                _routes.Add(route);
            }
        }
    }
    
    public class RigasSatiksmeRoute
    {
        public string Id { get; }
        
        public string Name { get; }


        public RigasSatiksmeRoute(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
    
    public class RigasSatiksmeTrips
    {
        public IEnumerable<RigasSatiksmeTrip> Trips => _trips.AsReadOnly();

        
        private readonly List<RigasSatiksmeTrip> _trips;

        
        public RigasSatiksmeTrips(string tripDataFileName, string stopDataFileName, RigasSatiksmeStops stops, RigasSatiksmeRoutes routes)
        {
            // TODO: tripDataFileName
            
            string[] lines = File.ReadAllLines(stopDataFileName);

            _trips = new List<RigasSatiksmeTrip>();

            List<RigasSatiksmeTripPoint> currentPoints = new List<RigasSatiksmeTripPoint>();
            string? currentTripId = null;
            
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

                RigasSatiksmeTripPoint newPoint = new RigasSatiksmeTripPoint(stop);

                if (currentTripId != tripId || // a new sequence
                    i == lines.Length) // last entry, final sequence
                {
                    RigasSatiksmeTrip trip = new RigasSatiksmeTrip(tripId, currentPoints);
                    _trips.Add(trip);

                    currentPoints = new List<RigasSatiksmeTripPoint>() { newPoint };
                    currentTripId = tripId;
                }
                else // continuing current sequence
                {
                    currentPoints.Add(newPoint);
                }
            }
        }
    }
    
    public class RigasSatiksmeTrip
    {
        public string Id { get; }

        public IEnumerable<RigasSatiksmeTripPoint> Points => _points.AsReadOnly();


        private List<RigasSatiksmeTripPoint> _points;


        public RigasSatiksmeTrip(string id, List<RigasSatiksmeTripPoint> points)
        {
            Id = id;
            _points = points;
        }
    }
    
    public class RigasSatiksmeTripPoint
    {
        public RigasSatiksmeStop Stop { get; }


        public RigasSatiksmeTripPoint(RigasSatiksmeStop stop)
        {
            Stop = stop;
        }
    }
}