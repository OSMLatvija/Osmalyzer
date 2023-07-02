using System.IO;

namespace Osmalyzer
{
    /// <summary>
    ///
    /// Example RS Data https://data.gov.lv/dati/lv/dataset/marsrutu-saraksti-rigas-satiksme-sabiedriskajam-transportam
    /// GTFS format Spec https://developers.google.com/transit/gtfs/reference/
    /// </summary>
    public class PublicTransportNetwork
    {
        public PublicTransportStops Stops { get; }
        
        public PublicTransportRoutes Routes { get; }
        
        public PublicTransportServices Services { get; }
        
        public PublicTransportTrips Trips { get; }
        
        public PublicTransportPoints Points { get; }


        public PublicTransportNetwork(string dataFolder)
        {
            Stops = new PublicTransportStops(Path.Combine(dataFolder, "stops.txt"));
            
            Routes = new PublicTransportRoutes(Path.Combine(dataFolder, "routes.txt"));
            
            Services = new PublicTransportServices(Path.Combine(dataFolder, "calendar.txt"));
            
            Trips = new PublicTransportTrips(Path.Combine(dataFolder, "trips.txt"), Routes, Services);
            
            Points = new PublicTransportPoints(Path.Combine(dataFolder, "stop_times.txt"), Stops, Trips);

            // Post-process

            foreach (PublicTransportRoute route in Routes.Routes)
            {
                foreach (PublicTransportService service in route.Services)
                {
                    foreach (PublicTransportTrip trip in service.Trips)
                    {
                        foreach (PublicTransportStop stop in trip.Stops)
                        {
                            switch (route.Type)
                            {
                                case "bus":
                                    stop.Bus = true;
                                    break;
                                
                                case "trolleybus":
                                    stop.Trolleybus = true;
                                    break;
                                
                                case "tram":
                                    stop.Tram = true;
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}