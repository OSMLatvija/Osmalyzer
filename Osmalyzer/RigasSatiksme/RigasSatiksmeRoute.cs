using System.Collections.Generic;

namespace Osmalyzer
{
    public class RigasSatiksmeRoute
    {
        public string Id { get; }
        
        public string Name { get; }

        public IEnumerable<RigasSatiksmeService> Services => _services.AsReadOnly();

        
        private readonly List<RigasSatiksmeService> _services = new List<RigasSatiksmeService>();

        
        public RigasSatiksmeRoute(string id, string name)
        {
            Id = id;
            Name = name;
        }


        public void AddService(RigasSatiksmeService service)
        {
            _services.Add(service);
        }
    }
}