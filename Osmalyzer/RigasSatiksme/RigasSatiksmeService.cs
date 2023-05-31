using System.Collections.Generic;

namespace Osmalyzer
{
    public class RigasSatiksmeService
    {
        public string Id { get; }

        public IEnumerable<RigasSatiksmeTrip> Trips => _trips.AsReadOnly();
        
        /// <summary>
        /// Note that multiple routes may run the same service, although likely with different points/stops.
        /// </summary>
        public IEnumerable<RigasSatiksmeRoute> Routes => _routes.AsReadOnly();


        private readonly List<RigasSatiksmeTrip> _trips = new List<RigasSatiksmeTrip>();
        
        private readonly List<RigasSatiksmeRoute> _routes = new List<RigasSatiksmeRoute>();


        public RigasSatiksmeService(string id)
        {
            Id = id;
        }

        
        public void AddTrip(RigasSatiksmeTrip trip)
        {
            _trips.Add(trip);
        }
        
        public void AddRoute(RigasSatiksmeRoute route)
        {
            _routes.Add(route);
        }
    }
}