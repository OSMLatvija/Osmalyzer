﻿namespace Osmalyzer
{
    public class RigasSatiksmeStop
    {
        public string Id { get; }

        public string Name { get; }
        
        public double Lat { get; }
        
        public double Lon { get; }
        
        public bool Bus { get; set; }
        
        public bool Trolleybus { get; set; }
        
        public bool Tram { get; set; }


        public RigasSatiksmeStop(string id, string name, double lat, double lon)
        {
            Id = id;
            Name = name;
            Lat = lat;
            Lon = lon;
        }
    }
}