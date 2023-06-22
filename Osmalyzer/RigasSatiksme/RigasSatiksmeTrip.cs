using System.Collections.Generic;

namespace Osmalyzer
{
    public class RigasSatiksmeTrip
    {
        public string Id { get; }
        
        public RigasSatiksmeService Service { get; }

        public IEnumerable<RigasSatiksmePoint> Points => _points.AsReadOnly();


        private readonly List<RigasSatiksmePoint> _points = new List<RigasSatiksmePoint>();


        public RigasSatiksmeTrip(string id, RigasSatiksmeService service)
        {
            Id = id;
            Service = service;
        }

        
        public void AddPoint(RigasSatiksmePoint point)
        {
            _points.Add(point);
        }


        public override string ToString()
        {
            return "Trip #" + Id + " for service #" + Service.Id;
        }
    }
}