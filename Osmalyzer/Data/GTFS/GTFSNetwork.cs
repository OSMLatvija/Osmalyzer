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
        
    public GTFSVisits Visits { get; }


    public GTFSNetwork(string dataPath)
    {
        Stops = new GTFSStops(Path.Combine(dataPath, "stops.txt"));
            
        Routes = new GTFSRoutes(Path.Combine(dataPath, "routes.txt"));
            
        Services = new GTFSServices(Path.Combine(dataPath, "calendar.txt"));
            
        Trips = new GTFSTrips(Path.Combine(dataPath, "trips.txt"), Routes, Services);
            
        Visits = new GTFSVisits(Path.Combine(dataPath, "stop_times.txt"), Stops, Trips);

        // Post-process

        AssignVehicleTypeToStops();

        DetermineStopPickupDropOffTypes();
    }


    public void CleanStopNames(Func<string, string> callback)
    {
        Stops.CleanNames(callback);
    }


    private void AssignVehicleTypeToStops()
    {
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
                            case GTFSRouteVehicleType.Bus:
                            case GTFSRouteVehicleType.NightBus:
                            case GTFSRouteVehicleType.Minibus:
                                stop.Bus = true;
                                break;
                                
                            case GTFSRouteVehicleType.Trolleybus:
                                stop.Trolleybus = true;
                                break;
                                
                            case GTFSRouteVehicleType.Tram:
                                stop.Tram = true;
                                break;
                            
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }
    }
    
    private void DetermineStopPickupDropOffTypes()
    {
        foreach (GTFSStop stop in Stops.Stops)
        {
            GTFSPickupType pickupType = GTFSPickupType.Unknown;
            
            foreach (GTFSVisit visit in stop.Visits)
            {
                if (visit.PickupType != GTFSPickupType.Unknown)
                {
                    if (pickupType == GTFSPickupType.Unknown)
                    {
                        pickupType = visit.PickupType;
                    }
                    else if (pickupType != visit.PickupType)
                    {
                        pickupType = GTFSPickupType.Mixed;
                        break; // multiple/conflicting values
                    }
                }
            }
            
            GTFSDropoffType dropOffType = GTFSDropoffType.Unknown;
            
            foreach (GTFSVisit visit in stop.Visits)
            {
                if (visit.DropOffType != GTFSDropoffType.Unknown)
                {
                    if (dropOffType == GTFSDropoffType.Unknown)
                    {
                        dropOffType = visit.DropOffType;
                    }
                    else if (dropOffType != visit.DropOffType)
                    {
                        dropOffType = GTFSDropoffType.Mixed;
                        break; // multiple/conflicting values
                    }
                }
            }
            
            stop.CommonPickupType = pickupType;
            stop.CommonDropOffType = dropOffType;
        }
    }
}