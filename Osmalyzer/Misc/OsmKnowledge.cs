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
    public static string GetFeatureLabel(OsmElement element, bool capitalize)
    {
        string fallbackLabel = element.ElementType switch
        {
            OsmElement.OsmElementType.Node     => "node",
            OsmElement.OsmElementType.Way      => "way",
            OsmElement.OsmElementType.Relation => "relation",
            _ => throw new ArgumentOutOfRangeException()
        };
        
        return GetFeatureLabel(element, fallbackLabel, capitalize);
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
            if (element is OsmRelation)
            {
                string? type = element.GetValue("type");

                if (type == "boundary")
                {
                    string? boundary = element.GetValue("boundary");

                    if (boundary == "administrative")
                    {
                        string? adminPlace = element.GetValue("place");

                        if (adminPlace != null)
                        {
                            switch (adminPlace)
                            {
                                case "country":       return "country boundary";
                                case "state":         return "state boundary";
                                case "region":        return "region boundary";
                                case "province":      return "province boundary";
                                case "district":      return "district boundary";
                                case "county":        return "county boundary";
                                case "subdistrict":   return "subdistrict boundary";
                                case "municipality":  return "municipality boundary";
                                case "civic_parish":  return "parish boundary";
                                case "city":          return "city boundary";
                                case "borough":       return "borough boundary";
                                case "suburb":        return "suburb boundary";
                                case "quarter":       return "quarter boundary";
                                case "neighbourhood": return "neighbourhood boundary";
                                case "village":       return "village boundary";
                                case "hamlet":        return "hamlet boundary";
                            }
                        }

                        return "admin boundary";
                    }
                }
            }
            
            // TODO: all the others
            
            string? amenity = element.GetValue("amenity");

            if (amenity != null)
            {
                switch (amenity)
                {
                    case "parking":      return "parking";
                    case "fuel":         return "fuel station";
                    case "kindergarten": return "kindergarten";
                    case "school":       return "school";
                    case "college":      return "college";
                    case "university":   return "university";
                }
            }        
        
            string? leisure = element.GetValue("leisure");

            if (leisure != null)
            {
                if (leisure == "fitness_station")
                {
                    if (element.HasKey("fitness_station"))
                        return "fitness equipment"; // subkey implies individual equipment, not a station as a whole

                    return "fitness station";
                }
                
                switch (leisure)
                {
                    case "pitch":           return "sports pitch";
                    case "park":            return "park";
                    case "playground":      return "playground";
                }
            }    
        
            string? place = element.GetValue("place");

            if (place != null)
            {
                switch (place)
                {
                    case "country":           return "country";
                    case "state":             return "state";
                    case "region":            return "region";
                    case "province":          return "province";
                    case "district":          return "district";
                    case "county":            return "county";
                    case "subdistrict":       return "subdistrict";
                    case "municipality":      return "municipality";
                    case "civic_parish":      return "parish";
                    case "city":              return "city";
                    case "borough":           return "borough";
                    case "suburb":            return "suburb";
                    case "quarter":           return "quarter";
                    case "neighbourhood":     return "neighbourhood";
                    case "city_block":        return "city block";
                    case "plot":              return "plot";
                    case "town":              return "town";
                    case "village":           return "village";
                    case "hamlet":            return "hamlet";
                    case "isolated_dwelling": return "isolated dwelling";
                    case "farm":              return "farm";
                    case "allotments":        return "allotments";
                    case "continent":         return "continent";
                    case "archipelago":       return "archipelago";
                    case "island":            return "island";
                    case "islet":             return "islet";
                    case "square":            return "square";
                    case "locality":          return "locality";
                    case "polder":            return "polder";
                    case "sea":               return "sea";
                    case "ocean":             return "ocean";
                }
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

            // Building as primary tag for last, since many primary tags can be on building, making it two primary tags and building is less important by itself  
            if (element.HasKey("building")) return "building";
            // todo: subtypes

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
            if (amenity 
                is "parking" 
                or "fuel" 
                or "kindergarten"
                or "school"
                or "college"
                or "university"
                )
                return ("amenity", amenity);
        }
        
        string? leisure = element.GetValue("leisure");

        if (leisure != null)
        {
            if (leisure == "fitness_station" && (!element.HasKey("fitness_station") || element.ElementType != OsmElement.OsmElementType.Node))
                return ("leisure", leisure); // fitness station is a special case, as it can be a single piece of equipment or a whole "station"
            
            if (leisure 
                is "pitch" 
                or "park" 
                or "playground")
                return ("leisure", leisure);
        }
        
        string? place = element.GetValue("place");

        if (place != null)
        {
            if (place 
                is "isolated_dwelling"
                or "country"
                or "state"
                or "region"
                or "province"
                or "district"
                or "county"
                or "subdistrict"
                or "municipality"
                or "city"
                or "borough"
                or "suburb"
                or "quarter"
                or "neighbourhood"
                or "city_block"
                or "plot"
                or "town"
                or "village"
                or "hamlet"
                or "farm"
                or "allotments"
                or "continent"
                or "archipelago"
                or "island"
                or "islet"
                or "square"
                or "locality"
                or "polder"
                or "sea"
                or "ocean"
                )
                return ("place", place);
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