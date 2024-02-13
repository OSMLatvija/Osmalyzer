namespace Osmalyzer;

public static class OsmKnowledge
{
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
            
            if (element.HasValue("amenity", "parking")) return "parking";
            if (element.HasValue("place", "square")) return "square";
            if (element.HasValue("amenity", "parking") && element.HasValue("area", "yes")) return "pedestrian area";

            return null;
        }
    }
}