﻿using System.IO;

namespace Osmalyzer
{
    /// <summary>
    ///
    /// RS Data https://data.gov.lv/dati/lv/dataset/marsrutu-saraksti-rigas-satiksme-sabiedriskajam-transportam
    /// Spec https://developers.google.com/transit/gtfs/reference/
    /// </summary>
    public class RigasSatiksmeNetwork
    {
        public RigasSatiksmeStops Stops { get; }
        
        public RigasSatiksmeRoutes Routes { get; }
        
        public RigasSatiksmeServices Services { get; }
        
        public RigasSatiksmeTrips Trips { get; }
        
        public RigasSatiksmePoints Points { get; }


        public RigasSatiksmeNetwork(string dataFolder)
        {
            Stops = new RigasSatiksmeStops(Path.Combine(dataFolder, "stops.txt"));
            
            Routes = new RigasSatiksmeRoutes(Path.Combine(dataFolder, "routes.txt"));
            
            Services = new RigasSatiksmeServices(Path.Combine(dataFolder, "calendar.txt"));
            
            Trips = new RigasSatiksmeTrips(Path.Combine(dataFolder, "trips.txt"), Routes, Services);
            
            Points = new RigasSatiksmePoints(Path.Combine(dataFolder, "stop_times.txt"), Stops, Trips);

            // Post-process

            foreach (RigasSatiksmeRoute route in Routes.Routes)
            {
                foreach (RigasSatiksmeService service in route.Services)
                {
                    foreach (RigasSatiksmeTrip trip in service.Trips)
                    {
                        foreach (RigasSatiksmeStop stop in trip.Stops)
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