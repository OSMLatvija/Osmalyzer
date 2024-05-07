namespace Osmalyzer;

public static class OsmKnowledge
{
    // todo: presets and not this hardcoded garbage
    
    [Pure]
    public static bool IsRoutableHighwayValue(string value)
    {
        return value
            is "motorway"
            or "trunk"
            or "primary"
            or "secondary"
            or "tertiary"
            or "unclassified"
            or "residential"

            or "motorway_link"
            or "trunk_link"
            or "primary_link"
            or "secondary_link"
            or "tertiary_link"

            or "living_street"
            or "service"
            or "pedestrian"
            or "track"

            or "footway"
            or "bridleway"
            or "steps"
            or "path"
            or "cycleway"

            or "crossing"
            or "bus_stop"
            or "platform";
    }

    [Pure]
    public static string GetFeatureLabel(OsmElement element, string fallbackValue, bool capitalize)
    {
        string? labelRaw = GetFeatureLabelRaw(element);
        
        if (labelRaw == null)
            return fallbackValue;

        if (capitalize)
            return char.ToUpper(labelRaw[0]) + labelRaw[1..];
        
        return labelRaw;
        
        static string? GetFeatureLabelRaw(OsmElement element)
        {
            // TODO: all the others
            
            if (element.HasKey("building")) return "building";
            
            if (element.HasValue("amenity", "parking")) return "parking";
            if (element.HasValue("place", "square")) return "square";
            
            string? amenity = element.GetValue("amenity");

            if (amenity != null)
            {
                switch (amenity)
                {
                    case "parking":      return "parking";
                    case "fuel":         return "fuel station";
                    case "kindergarten": return "kindergarten";
                }
            }        
        
            string? leisure = element.GetValue("leisure");

            if (leisure != null)
            {
                switch (leisure)
                {
                    case "pitch":      return "sports pitch";
                    case "park":         return "park";
                    case "playground": return "playground";
                }
            }    
        
            string? place = element.GetValue("place");

            if (place != null)
            {
                if (place == "square") return "square";
            }
        
            string? highway = element.GetValue("highway");

            if (highway != null)
            {
                if (element.HasValue("area", "yes"))
                {
                    if (highway == "pedestrian") return "pedestrian area";
                }
            }

            return null;
        }
    }

    [Pure]
    public static bool IsAreaFeature(OsmElement element)
    {
        return GetAreaFeature(element) != null;
    }

    [Pure]
    public static (string key, string value)? GetAreaFeature(OsmElement element)
    {
        string? amenity = element.GetValue("amenity");

        if (amenity != null)
        {
            if (amenity is "parking" or "fuel" or "kindergarten")
                return ("amenity", amenity);
        }
        
        string? leisure = element.GetValue("leisure");

        if (leisure != null)
        {
            if (leisure is "pitch" or "park" or "playground")
                return ("leisure", leisure);
        }

        return null;
    }

    [Pure]
    public static bool AreSameAreaFeatures(OsmElement element1, OsmElement element2)
    {
        (string key, string value)? areaFeature1 = GetAreaFeature(element1);
        (string key, string value)? areaFeature2 = GetAreaFeature(element2);

        return areaFeature1 != null &&
               areaFeature2 != null &&
               areaFeature1.Value.value == areaFeature2.Value.value &&
               areaFeature1.Value.key == areaFeature2.Value.key;
    }
}