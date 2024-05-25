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

                    return "road area";
                }
                
                if (highway == "motorway") return "motorway";
                if (highway == "trunk") return "trunk road";
                if (highway == "primary") return "primary road";
                if (highway == "secondary") return "secondary road";
                if (highway == "tertiary") return "tertiary road";
                if (highway == "unclassified") return "unclassified road";
                if (highway == "residential") return "residential road";
                if (highway == "motorway_link") return "motorway link";
                if (highway == "trunk_link") return "trunk road link";
                if (highway == "primary_link") return "primary road link";
                if (highway == "secondary_link") return "secondary road link";
                if (highway == "tertiary_link") return "tertiary road link";
                if (highway == "living_street") return "living street";
                if (highway == "service") return "service road";
                if (highway == "pedestrian") return "pedestrian road";
                if (highway == "track") return "track";
                if (highway == "footway") return "footway";
                if (highway == "bridleway") return "bridleway";
                if (highway == "steps") return "steps";
                if (highway == "path") return "path";
                if (highway == "cycleway") return "cycleway";
                if (highway == "crossing") return "crossing";
                if (highway == "platform") return "psv platform";
            }
        
            string? barrier = element.GetValue("barrier");

            if (barrier != null)
            {
                if (barrier == "fence")
                {
                    string? fenceType = element.GetValue("fence_type");

                    if (fenceType == "railing") return "railing";
                        
                    return "fence";
                }
                
                if (barrier == "wall") return "wall";
                if (barrier == "guard_rail") return "guard rail";
                if (barrier == "handrail") return "handrail";
                if (barrier == "hedge") return "hedge";
                if (barrier == "retaining_wall") return "retaining wall";
                if (barrier == "ditch") return "trench";
                
                if (barrier == "block") return "block";
                if (barrier == "bollard") return "bollard";
                if (barrier == "planter") return "planter";
                
                if (barrier == "gate") return "gate";
                if (barrier == "wicket_gate") return "wicket gate";
                if (barrier == "lift_gate") return "lift gate";
                if (barrier == "swing_gate") return "swing gate";
                if (barrier == "sliding_gate") return "sliding gate";
                if (barrier == "kissing_gate") return "kissing gate";
                if (barrier == "entrance") return "entrance";
                if (barrier == "cattle_grid") return "cattle grid";
                if (barrier == "chain") return "chain barrier";
                if (barrier == "sally_port") return "sally port";

                return "barrier";
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