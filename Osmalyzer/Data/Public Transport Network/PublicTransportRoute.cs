using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Osmalyzer;

public class PublicTransportRoute
{
    public string Id { get; }
        
    public string Name { get; }
        
    public string Number { get; }

    public string Type { get; }

    public string CleanType { get; }

    public IEnumerable<PublicTransportService> Services => _services.AsReadOnly();


    private readonly List<PublicTransportService> _services = new List<PublicTransportService>();

        
    public PublicTransportRoute(string id, string name, string number, string type)
    {
        Id = id;
        Name = name;
        Number = number;
        Type = type;
        CleanType = TypeToCleanType(type);
    }


    public void AddService(PublicTransportService service)
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
            "minibus"    => "Minibus",
            _            => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}