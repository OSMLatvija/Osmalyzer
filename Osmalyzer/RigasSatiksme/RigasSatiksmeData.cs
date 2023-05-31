using System;
using System.IO;

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
        
        public RigasSatiksmeServices Services { get; }
        
        public RigasSatiksmeTrips Trips { get; }
        
        public RigasSatiksmePoints Points { get; }


        public RigasSatiksmeData(string dataFolder)
        {
            Stops = new RigasSatiksmeStops(Path.Combine(dataFolder, "stops.txt"));
            
            Routes = new RigasSatiksmeRoutes(Path.Combine(dataFolder, "routes.txt"));
            
            Services = new RigasSatiksmeServices(Path.Combine(dataFolder, "trips.txt"), Routes);
            
            Trips = new RigasSatiksmeTrips(Path.Combine(dataFolder, "trips.txt"), Routes, Services);
            
            Points = new RigasSatiksmePoints(Path.Combine(dataFolder, "stop_times.txt"), Stops, Trips);
        }
    }
}