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
}