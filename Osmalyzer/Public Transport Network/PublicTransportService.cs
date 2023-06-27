using System.Collections.Generic;

namespace Osmalyzer
{
    public class PublicTransportService
    {
        public string Id { get; }

        public IEnumerable<PublicTransportTrip> Trips => _trips.AsReadOnly();
        
        /// <summary>
        /// Note that multiple routes may run the same service, although likely with different points/stops.
        /// </summary>
        public IEnumerable<PublicTransportRoute> Routes => _routes.AsReadOnly();


        private readonly List<PublicTransportTrip> _trips = new List<PublicTransportTrip>();
        
        private readonly List<PublicTransportRoute> _routes = new List<PublicTransportRoute>();


        public PublicTransportService(string id)
        {
            Id = id;
        }

        
        public void AddTrip(PublicTransportTrip trip)
        {
            _trips.Add(trip);
        }
        
        public void AddRoute(PublicTransportRoute route)
        {
            _routes.Add(route);
        }
    }
}