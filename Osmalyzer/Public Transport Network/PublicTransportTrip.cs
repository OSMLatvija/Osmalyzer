using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer
{
    public class PublicTransportTrip
    {
        public string Id { get; }
        
        public PublicTransportService Service { get; }

        public IEnumerable<PublicTransportPoint> Points => _points.AsReadOnly();
        
        public IEnumerable<PublicTransportStop> Stops => _points.Select(p => p.Stop);


        private readonly List<PublicTransportPoint> _points = new List<PublicTransportPoint>();


        public PublicTransportTrip(string id, PublicTransportService service)
        {
            Id = id;
            Service = service;
        }

        
        public void AddPoint(PublicTransportPoint point)
        {
            _points.Add(point);
        }


        public override string ToString()
        {
            return "Trip #" + Id + " for service #" + Service.Id;
        }
    }
}