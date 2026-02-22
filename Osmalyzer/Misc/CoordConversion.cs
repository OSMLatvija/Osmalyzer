using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Osmalyzer;

public static class CoordConversion
{
    private static readonly ICoordinateTransformation _LKS92ToWGS84;
    
    private static readonly ICoordinateTransformation _LKS2020ToWGS84;

    
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
            new ProjectionParameter("false_northing", -6000000)
        ];

        IProjection? tm = csFactory.CreateProjection("Transverse_Mercator", "Transverse_Mercator", projParams);

        // PROJCS["LKS-92 / Latvia TM",
        //     GEOGCS["LKS-92",
        //         DATUM["Latvian_geodetic_coordinate_system_1992",
        //             SPHEROID["GRS 1980",6378137,298.257222101],
        //             TOWGS84[0,0,0,0,0,0,0]],
        //         PRIMEM["Greenwich",0,
        //             AUTHORITY["EPSG","8901"]],
        //         UNIT["degree",0.0174532925199433,
        //             AUTHORITY["EPSG","9122"]],
        //         AUTHORITY["EPSG","4661"]],
        //     PROJECTION["Transverse_Mercator"],
        //     PARAMETER["latitude_of_origin",0],
        //     PARAMETER["central_meridian",24],
        //     PARAMETER["scale_factor",0.9996],
        //     PARAMETER["false_easting",500000],
        //     PARAMETER["false_northing",-6000000],
        //     UNIT["metre",1,
        //         AUTHORITY["EPSG","9001"]],
        //     AUTHORITY["EPSG","3059"]]        
        
        ProjectedCoordinateSystem? lks92 = csFactory.CreateProjectedCoordinateSystem(
            "LKS-92 / Latvia TM (EPSG:3059)",
            GeographicCoordinateSystem.WGS84, // ETRS89≈WGS84 here
            tm,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North)
        );

        _LKS92ToWGS84 = new CoordinateTransformationFactory().CreateFromCoordinateSystems(lks92, wgs84);
        
        // PROJCS["LKS-2020 / Latvia TM",
        //     GEOGCS["LKS-2020",
        //         DATUM["Latvian_coordinate_system_2020",
        //             SPHEROID["GRS 1980",6378137,298.257222101],
        //             TOWGS84[0,0,0,0,0,0,0]],
        //         PRIMEM["Greenwich",0,
        //             AUTHORITY["EPSG","8901"]],
        //         UNIT["degree",0.0174532925199433,
        //             AUTHORITY["EPSG","9122"]],
        //         AUTHORITY["EPSG","10305"]],
        //     PROJECTION["Transverse_Mercator"],
        //     PARAMETER["latitude_of_origin",0],
        //     PARAMETER["central_meridian",24],
        //     PARAMETER["scale_factor",0.9996],
        //     PARAMETER["false_easting",500000],
        //     PARAMETER["false_northing",-6000000],
        //     UNIT["metre",1,
        //         AUTHORITY["EPSG","9001"]],
        //     AUTHORITY["EPSG","10306"]]        
        
        ProjectedCoordinateSystem? lks2020 = csFactory.CreateProjectedCoordinateSystem(
            "LKS-2020 / Latvia TM (EPSG:10306)",
            GeographicCoordinateSystem.WGS84, // ETRS89≈WGS84 here
            tm, // yes, this has the same projection parameters as LKS-92, it's the source data that has to "convert", although in practice there would be no difference for us
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North)
        );
        
        _LKS2020ToWGS84 = new CoordinateTransformationFactory().CreateFromCoordinateSystems(lks2020, wgs84);
    }
    
    
    [Pure]
    public static (double lat, double lon) LKS92ToWGS84(double easting, double northing)
    {
        double[]? lonLat = _LKS92ToWGS84.MathTransform.Transform([ easting, northing ]);
        
        return (lonLat[1], lonLat[0]);
    }
    
    // TODO: UPDATE WHEN ALL SOURCES START USING LKS-2020
    // https://www.lgia.gov.lv/lv/lks-2020
    // this will be an assumption anyway if source doesn't say it, because coords have same projection
    
    [Pure]
    public static (double lat, double lon) LKS2020ToWGS84(double easting, double northing)
    {
        double[]? lonLat = _LKS2020ToWGS84.MathTransform.Transform([ easting, northing ]);
        
        return (lonLat[1], lonLat[0]);
    }
}