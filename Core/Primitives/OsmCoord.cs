using System;

namespace Osmalyzer;

public struct OsmCoord : IEquatable<OsmCoord>
{
    public readonly double lat;
        
    public readonly double lon;


    public string OsmUrl => @"https://www.openstreetmap.org/#map=19/" + lat.ToString("F5") + @"/" + lon.ToString("F5");

        
    public OsmCoord(double lat, double lon)
    {
        this.lat = lat;
        this.lon = lon;
    }


    #region ==
        
    public override bool Equals(object? obj)
    {
        return obj is OsmCoord other && Equals(other);
    }

    public bool Equals(OsmCoord other)
    {
        const double epsilon = 0.00001; // about a meter
            
        return 
            Math.Abs(lat - other.lat) < epsilon && 
            Math.Abs(lon - other.lon) < epsilon;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (lat.GetHashCode() * 397) ^ lon.GetHashCode();
        }
    }

    public static bool operator ==(OsmCoord left, OsmCoord right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(OsmCoord left, OsmCoord right)
    {
        return !left.Equals(right);
    }
        
    #endregion


    public override string ToString()
    {
        return lat.ToString("F5") + @"/" + lon.ToString("F5");
    }
}