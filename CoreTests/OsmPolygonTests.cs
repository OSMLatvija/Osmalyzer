using NUnit.Framework;

namespace Osmalyzer;

[TestFixture]
public class OsmPolygonTests
{
    [Test]
    public void TestGetOverlapCoveragePercent_ExactMatch()
    {
        // Arrange
        List<OsmCoord> coords = new List<OsmCoord>
        {
            new OsmCoord(56.0, 24.0),
            new OsmCoord(56.0, 24.1),
            new OsmCoord(56.1, 24.1),
            new OsmCoord(56.1, 24.0),
            new OsmCoord(56.0, 24.0) // close the polygon
        };

        OsmPolygon polygon1 = new OsmPolygon(coords);
        OsmPolygon polygon2 = new OsmPolygon(coords); // exact copy

        // Act
        double coverage = polygon1.GetOverlapCoveragePercent(polygon2);

        // Assert
        Console.WriteLine($"Coverage: {coverage * 100}%");
        Assert.That(coverage, Is.GreaterThan(0.99), "Exact match should have ~100% coverage");
    }

    [Test]
    public void TestGetOverlapCoveragePercent_ExactMatch_WithoutDuplicateClose()
    {
        // Arrange
        List<OsmCoord> coords = new List<OsmCoord>
        {
            new OsmCoord(56.0, 24.0),
            new OsmCoord(56.0, 24.1),
            new OsmCoord(56.1, 24.1),
            new OsmCoord(56.1, 24.0)
            // NOT closing the polygon here
        };

        OsmPolygon polygon1 = new OsmPolygon(coords);
        OsmPolygon polygon2 = new OsmPolygon(coords); // exact copy

        // Act
        double coverage = polygon1.GetOverlapCoveragePercent(polygon2);

        // Assert
        Console.WriteLine($"Coverage (no duplicate close): {coverage * 100}%");
        Assert.That(coverage, Is.GreaterThan(0.99), "Exact match should have ~100% coverage");
    }

    [Test]
    public void TestGetOverlapCoveragePercent_NoOverlap()
    {
        // Arrange
        List<OsmCoord> coords1 = new List<OsmCoord>
        {
            new OsmCoord(56.0, 24.0),
            new OsmCoord(56.0, 24.1),
            new OsmCoord(56.1, 24.1),
            new OsmCoord(56.1, 24.0),
            new OsmCoord(56.0, 24.0)
        };

        List<OsmCoord> coords2 = new List<OsmCoord>
        {
            new OsmCoord(57.0, 25.0),
            new OsmCoord(57.0, 25.1),
            new OsmCoord(57.1, 25.1),
            new OsmCoord(57.1, 25.0),
            new OsmCoord(57.0, 25.0)
        };

        OsmPolygon polygon1 = new OsmPolygon(coords1);
        OsmPolygon polygon2 = new OsmPolygon(coords2);

        // Act
        double coverage = polygon1.GetOverlapCoveragePercent(polygon2);

        // Assert
        Assert.That(coverage, Is.LessThan(0.01), "No overlap should have ~0% coverage");
    }
}

