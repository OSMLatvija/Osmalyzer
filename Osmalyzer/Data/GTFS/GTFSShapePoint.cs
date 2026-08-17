namespace Osmalyzer;

public class GTFSShapePoint
{
    public GTFSShape Shape { get; }

    public OsmCoord Coord { get; }

    public int Sequence { get; }

    public double? DistanceTraveled { get; }

    
    public GTFSShapePoint(GTFSShape shape, OsmCoord coord, int sequence, double? distanceTraveled)
    {
        Shape = shape;
        Coord = coord;
        Sequence = sequence;
        DistanceTraveled = distanceTraveled;
    }
}