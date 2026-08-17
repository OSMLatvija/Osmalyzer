namespace Osmalyzer;

public class GTFSShapes
{
    public IEnumerable<GTFSShape> Shapes => _shapes.Values.AsEnumerable();

    
    private readonly Dictionary<string, GTFSShape> _shapes;

    
    public GTFSShapes(string dataFileName)
    {
        _shapes = new Dictionary<string, GTFSShape>(StringComparer.OrdinalIgnoreCase);
        
        Parse(dataFileName);
    }


    [Pure]
    public GTFSShape? GetShape(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _shapes.TryGetValue(id, out GTFSShape? shape) ? shape : null;
    }

    
    private void Parse(string dataFileName)
    {
        string[] lines = File.ReadAllLines(dataFileName);

        if (lines.Length < 2)
            return;

        string header = lines[0];
        string[] headers = header.Split(',').Select(s => s.Trim()).ToArray();

        int shapeIdIndex = headers.IndexOf("shape_id");
        int shapeLatIndex = headers.IndexOf("shape_pt_lat");
        int shapeLonIndex = headers.IndexOf("shape_pt_lon");
        int shapeSequenceIndex = headers.IndexOf("shape_pt_sequence");
        int shapeDistanceIndex = Array.IndexOf(headers, "shape_dist_traveled");

        if (shapeIdIndex == -1) throw new Exception($"Missing required column 'shape_id' in {dataFileName}");
        if (shapeLatIndex == -1) throw new Exception($"Missing required column 'shape_pt_lat' in {dataFileName}");
        if (shapeLonIndex == -1) throw new Exception($"Missing required column 'shape_pt_lon' in {dataFileName}");
        if (shapeSequenceIndex == -1) throw new Exception($"Missing required column 'shape_pt_sequence' in {dataFileName}");

        for (int i = 1; i < lines.Length; i++)
        {
            string row = lines[i];
            List<string> segments = row.Split(',').Select(s => s.Trim()).ToList();

            if (segments.Count <= shapeIdIndex || segments.Count <= shapeLatIndex || segments.Count <= shapeLonIndex || segments.Count <= shapeSequenceIndex)
                continue;

            string id = segments[shapeIdIndex];
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (!double.TryParse(segments[shapeLatIndex], out double lat))
                continue;
            if (!double.TryParse(segments[shapeLonIndex], out double lon))
                continue;
            if (!int.TryParse(segments[shapeSequenceIndex], out int sequence))
                continue;

            double? distanceTraveled = null;
            if (shapeDistanceIndex != -1 && shapeDistanceIndex < segments.Count)
            {
                string distanceValue = segments[shapeDistanceIndex];
                if (!string.IsNullOrWhiteSpace(distanceValue) && double.TryParse(distanceValue, out double distance))
                    distanceTraveled = distance;
            }

            GTFSShape shape = GetOrCreateShape(id);
            shape.AddPoint(new GTFSShapePoint(shape, new OsmCoord(lat, lon), sequence, distanceTraveled));
        }
    }

    private GTFSShape GetOrCreateShape(string id)
    {
        if (_shapes.TryGetValue(id, out GTFSShape? shape))
            return shape;

        shape = new GTFSShape(id);
        _shapes.Add(id, shape);
        return shape;
    }
}
