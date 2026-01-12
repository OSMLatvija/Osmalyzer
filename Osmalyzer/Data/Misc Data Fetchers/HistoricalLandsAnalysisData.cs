using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Osmalyzer;

[UsedImplicitly]
public class HistoricalLandsAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Historical Lands";

    public override string ReportWebLink => @"https://data.gov.lv/dati/dataset/vesturiskas-zemes/resource/ae8ce5f6-2120-430e-a2fb-1fd7df1e7b85";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "historical-lands";


    public List<HistoricalLand> HistoricalLands { get; private set; } = null!; // only null before prepared


    private string ExtractionFolder => "HistoricalLands";


    protected override void Download()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct reading/scraping
            ReportWebLink,
            true
        );

        // Find the download link for vesturiskas_zemes.zip
        Match urlMatch = Regex.Match(result, @"href=""(https://data\.gov\.lv/dati/dataset/[^/]*/resource/[^/]*/download/vesturiskas_zemes\.zip)""");

        if (!urlMatch.Success) throw new Exception("Could not find the download URL for Historical Lands (Vēsturiskās zemes) shapefile.");

        string url = urlMatch.Groups[1].ToString();

        WebsiteBrowsingHelper.DownloadPage( // data.gov.lv seems to not like direct download/scraping
            url,
            Path.Combine(CacheBasePath, DataFileIdentifier + @".zip")
        );
    }

    protected override void DoPrepare()
    {
        // Data comes in a zip file, so unzip
        
        ZipHelper.ExtractZipFile(
            Path.Combine(CacheBasePath, DataFileIdentifier + @".zip"),
            Path.GetFullPath(ExtractionFolder)
        );
        
        // Parse shapefile
        
        string projectionfilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Vesturiskas_zemes.prj");
        CoordinateSystem ourWkt = new CoordinateSystemFactory().CreateFromWkt(File.ReadAllText(projectionfilePath));
        
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
        
        ICoordinateTransformation coordTransformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(
            ourWkt, 
            wgs84
        );

        string shapefilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Vesturiskas_zemes.shp");

        using ShapefileDataReader shapefileReader = new ShapefileDataReader(shapefilePath, GeometryFactory.Default);

        HistoricalLands = [ ];
        
        while (shapefileReader.Read())
        {
            Geometry geometry = shapefileReader.Geometry;
            
            // Process shape
            
            Point centroid = geometry.Centroid;

            (double lon, double lat) = coordTransformation.MathTransform.Transform(centroid.X, centroid.Y);

            OsmCoord coord = new OsmCoord(lat, lon);
            
            // Process columns
            // Fields: id, code, name
            
            string id = shapefileReader["id"].ToString() ?? throw new Exception("Historical land in data without an id");
            string code = shapefileReader["code"].ToString() ?? throw new Exception("Historical land in data without a code");
            string name = shapefileReader["name"].ToString() ?? throw new Exception("Historical land in data without a name");

            // Process boundary

            OsmMultiPolygon boundary = OsmMultiPolygon.FromNTSGeometry(
                geometry,
                (x, y) => coordTransformation.MathTransform.Transform(x, y)
            );
            
            // Entry

            HistoricalLand historicalLand = new HistoricalLand(
                id,
                code,
                name,
                coord,
                boundary
            );

            HistoricalLands.Add(historicalLand);
        }
    }
}
