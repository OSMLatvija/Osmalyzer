using System.Collections.Generic;

namespace Osmalyzer
{
    public class RigasSatiksmeService
    {
        public string Id { get; }

        public IEnumerable<RigasSatiksmeTrip> Trips => _trips.AsReadOnly();


        private readonly List<RigasSatiksmeTrip> _trips = new List<RigasSatiksmeTrip>();


        public RigasSatiksmeService(string id)
        {
            Id = id;
        }

        
        public void AddTrip(RigasSatiksmeTrip trip)
        {
            _trips.Add(trip);
        }
    }
}