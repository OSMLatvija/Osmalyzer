using System.Diagnostics;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Osmalyzer;

[UsedImplicitly]
public class AddressGeodataAnalysisData : AnalysisData 
    // todo: , IDatedAnalysisData - need to parse
{
    public override string Name => "Address Geospatial data";

    public override string ReportWebLink => @"https://data.gov.lv/dati/dataset/varis-atvertie-dati/resource/b643b1b3-223f-4394-9beb-18524f8b0b82";

    public override bool NeedsPreparation => true;


    public List<Village> Villages { get; private set; } = null!; // only null before prepared


    protected override string DataFileIdentifier => "addresses-geo";


    private string ExtractionFolder => "ADRGEO";


    protected override void Download()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct reading/scraping
            ReportWebLink, 
            true
        );

        Match urlMatch = Regex.Match(result, @"href=""(https:\/\/data\.gov\.lv\/dati\/dataset\/[^""]+?aw_shp\.zip)""");

        if (!urlMatch.Success) throw new Exception("Could not find the download URL for data file.");
        
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
        
        // Parse
        
        string projectionfilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Ciemi.prj");
        CoordinateSystem ourWkt = new CoordinateSystemFactory().CreateFromWkt(File.ReadAllText(projectionfilePath));
            
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
            
        ICoordinateTransformation coordTransformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(
            ourWkt, 
            wgs84
        );

        string shapefilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Ciemi.shp");

        using ShapefileDataReader shapefileReader = new ShapefileDataReader(shapefilePath, GeometryFactory.Default);

        DbaseFileHeader dbaseHeader = shapefileReader.DbaseHeader;

#if !REMOTE_EXECUTION
        foreach (DbaseFieldDescriptor headerField in dbaseHeader.Fields)
            Debug.WriteLine(
                $"Field: {headerField.Name}, Type: {headerField.Type.Name}"
            );
#endif

        // Read shapes
            
        while (shapefileReader.Read())
        {
            Geometry geometry = shapefileReader.Geometry;

            // Process shape
                
            Point centroid = geometry.Centroid;

            (double lon, double lat) = coordTransformation.MathTransform.Transform(centroid.X, centroid.Y);

            OsmCoord coord = new OsmCoord(lat, lon);
                
            string name = "????";
            
            Villages.Add(
                new Village(
                    coord,
                    name
                )
            );
        }
    }
}