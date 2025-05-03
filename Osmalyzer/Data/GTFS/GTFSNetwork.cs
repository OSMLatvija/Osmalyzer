namespace Osmalyzer;

/// <summary>
///
/// Example RS Data https://data.gov.lv/dati/lv/dataset/marsrutu-saraksti-rigas-satiksme-sabiedriskajam-transportam
/// GTFS format Spec https://developers.google.com/transit/gtfs/reference/
/// Tech docs https://gtfs.org/documentation/schedule/reference/
/// </summary>
public class GTFSNetwork
{
    public GTFSStops Stops { get; }
        
    public GTFSRoutes Routes { get; }
        
    public GTFSServices Services { get; }
        
    public GTFSTrips Trips { get; }
        
    public GTFSPoints Points { get; }


    public GTFSNetwork(string dataPath)
    {
        Stops = new GTFSStops(Path.Combine(dataPath, "stops.txt"));
            
        Routes = new GTFSRoutes(Path.Combine(dataPath, "routes.txt"));
            
        Services = new GTFSServices(Path.Combine(dataPath, "calendar.txt"));
            
        Trips = new GTFSTrips(Path.Combine(dataPath, "trips.txt"), Routes, Services);
            
        Points = new GTFSPoints(Path.Combine(dataPath, "stop_times.txt"), Stops, Trips);

        // Post-process

        foreach (GTFSRoute route in Routes.Routes)
        {
            foreach (GTFSService service in route.Services)
            {
                foreach (GTFSTrip trip in service.Trips)
                {
                    foreach (GTFSStop stop in trip.Stops)
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