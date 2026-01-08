using System.Globalization;

namespace WikidataSharp;

public struct WikidataCoord
{
    public double Latitude { get; }

    public double Longitude { get; }

    
    public WikidataCoord(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    
    public static WikidataCoord? TryParse(string rawValue)
    {
        // "Point(21.690085 57.01969)"
        
        if (!rawValue.StartsWith("Point("))
            return null;
        
        int spaceIndex = rawValue.IndexOf(' ', 6);
        if (spaceIndex < 0)
            return null;
        
        int endIndex = rawValue.IndexOf(')', spaceIndex);
        if (endIndex < 0)
            return null;
        if (endIndex != rawValue.Length - 1)
            return null;
        
        string latStr = rawValue[(spaceIndex + 1)..endIndex];
        string lonStr = rawValue[6..spaceIndex];
        
        if (!double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude))
            return null;
        if (!double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude))
            return null;
        
        return new WikidataCoord(latitude, longitude);
    }
}