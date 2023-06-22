using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Osmalyzer
{
    public class RigasSatiksmeRoute
    {
        public string Id { get; }
        
        public string Name { get; }
        
        public string Number { get; }

        public string Type { get; }

        public string CleanType { get; }

        public IEnumerable<RigasSatiksmeService> Services => _services.AsReadOnly();


        private readonly List<RigasSatiksmeService> _services = new List<RigasSatiksmeService>();

        
        public RigasSatiksmeRoute(string id, string name, string number, string type)
        {
            Id = id;
            Name = name;
            Number = number;
            Type = type;
            CleanType = TypeToCleanType(type);
        }


        public void AddService(RigasSatiksmeService service)
        {
            _services.Add(service);
        }

        
        [Pure]
        private static string TypeToCleanType(string type)
        {
            return type switch
            {
                "bus"        => "Bus",
                "trolleybus" => "Trolleybus",
                "tram"       => "Tram",
                _            => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}