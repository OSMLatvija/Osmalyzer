using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Osmalyzer;

public static class CoordConversion
{
    private static readonly ICoordinateTransformation _LKS92ToWGS84;

    
    static CoordConversion()
    {
        CoordinateSystemFactory csFactory = new CoordinateSystemFactory();
        GeographicCoordinateSystem? wgs84 = GeographicCoordinateSystem.WGS84;

        List<ProjectionParameter> projParams =
        [
            new ProjectionParameter("latitude_of_origin", 0),
            new ProjectionParameter("central_meridian", 24),
            new ProjectionParameter("scale_factor", 0.9996),
            new ProjectionParameter("false_easting", 500000),
            new ProjectionParameter("false_northing", 0)
        ];

        IProjection? tm = csFactory.CreateProjection("Transverse_Mercator", "Transverse_Mercator", projParams);

        ProjectedCoordinateSystem? lks92 = csFactory.CreateProjectedCoordinateSystem(
            "LKS-92 / Latvia TM (EPSG:3059)",
            GeographicCoordinateSystem.WGS84, // ETRS89≈WGS84 here
            tm,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North)
        );

        _LKS92ToWGS84 = new CoordinateTransformationFactory().CreateFromCoordinateSystems(lks92, wgs84);
    }
    
    
    [Pure]
    public static (double lat, double lon) LKS92ToWGS84(double easting, double northing)
    {
        double[]? lonLat = _LKS92ToWGS84.MathTransform.Transform([ easting, northing ]);
        
        return (lonLat[1], lonLat[0]);
    }
}