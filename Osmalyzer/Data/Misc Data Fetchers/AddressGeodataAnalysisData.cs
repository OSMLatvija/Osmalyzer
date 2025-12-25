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

        ParseVillages();
        ParseHamlets();
    }

    private void ParseVillages()
    {
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
                $"Village: Field: {headerField.Name}, Type: {headerField.Type.Name}"
            );
        
        // `KODS` (Int32) -  Attiecīgā adresācijas objekta kods
        // `TIPS_CD` (Int32) -  Adresācijas objekta tipa kods (skatīt 1. pielikumu (106 = Ciems))
        // `NOSAUKUMS` (String) -  Adresācijas objekta aktuālais nosaukums
        // `VKUR_CD` (Int32) -  Tā adresācijas objekta kods, kuram hierarhiski pakļauts attiecīgais adresācijas objekts
        // `VKUR_TIPS` (Int32) -  Tā adresācijas objekta tipa kods (skatīt 1. pielikumu), kuram hierarhiski pakļauts attiecīgais adresācijas objekts
        // `APSTIPR` (String) -  Burts “Y” norāda vai adresācijas objekts ir apstiprināts
        // `APST_PAK` (Int32) -  Adresācijas objekta apstiprinājuma pakāpe (skatīt 3. pielikumu)
        // `STATUSS` (String) -  Adresācijas objekta statuss: EKS – eksistējošs
        // `SORT_NOS` (String) -  Kārtošanas nosacījums adresācijas objekta nosaukumam (ja nosaukumā ir tikai teksts, kārtošanas nosacījums ir identisks nosaukumam)
        // `DAT_SAK` (String) -  Adresācijas objekta izveidošanas vai pirmreizējās reģistrācijas datums, ja nav zināms precīzs adresācijas objekta izveides datums
        // `DAT_MOD` (String) -  Datums un laiks, kad pēdējo reizi informācijas sistēmā tehniski modificēts ieraksts/ dati par adresācijas objektu (piemēram, aktualizēts statuss, apstiprinājuma pakāpe, pievienots atribūts u.c.) vai mainīts pilnais adreses pieraksts
        // `DAT_BEIG` (String) -  Adresācijas objekta likvidācijas datums, ja adresācijas objekts beidza pastāvēt
        // `ATRIB` (String) -  ATVK kods
        // `STD` (String) -  Pilnais adreses pieraksts
#endif

        // Read shapes

        Villages = [ ];
            
        while (shapefileReader.Read())
        {
            Geometry geometry = shapefileReader.Geometry;
            
            // Process shape
                
            Point centroid = geometry.Centroid;

            (double lon, double lat) = coordTransformation.MathTransform.Transform(centroid.X, centroid.Y);

            OsmCoord coord = new OsmCoord(lat, lon);
            
            // Process columns
            
            string status = shapefileReader["STATUSS"].ToString() ?? throw new Exception("Village in data without a status");
            string approved = shapefileReader["APSTIPR"].ToString() ?? throw new Exception("Village in data without an approval status");
            string name = shapefileReader["NOSAUKUMS"].ToString() ?? throw new Exception("Village in data without a name");
            string address = shapefileReader["STD"].ToString() ?? throw new Exception("Village in data without a full address");
            string id = shapefileReader["KODS"].ToString() ?? throw new Exception("Village in data without a code");
            
            bool isValid = status == "EKS" && approved == "Y";

            // Process boundary

            List<OsmCoord> coords = [ ];

            foreach (Coordinate geometryCoord in geometry.Coordinates)
            {
                (double lonB, double latB) = coordTransformation.MathTransform.Transform(geometryCoord.X, geometryCoord.Y);
                
                coords.Add(new OsmCoord(latB, lonB));
            }
            
            OsmPolygon boundary = new OsmPolygon(coords);
            
            // Entry
           
            Villages.Add(
                new Village(
                    isValid,
                    id,
                    coord,
                    name,
                    address,
                    false,
                    boundary
                )
            );
        }
    }

    private void ParseHamlets()
    {
        // Hamlets (Mazciemi) come as points and share attribute schema with villages
        string projectionfilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Mazciemi.prj");
        CoordinateSystem ourWkt = new CoordinateSystemFactory().CreateFromWkt(File.ReadAllText(projectionfilePath));
        
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
        
        ICoordinateTransformation coordTransformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(
            ourWkt,
            wgs84
        );

        string shapefilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Mazciemi.shp");

        using ShapefileDataReader shapefileReader = new ShapefileDataReader(shapefilePath, GeometryFactory.Default);

        DbaseFileHeader dbaseHeader = shapefileReader.DbaseHeader;

#if !REMOTE_EXECUTION
        foreach (DbaseFieldDescriptor headerField in dbaseHeader.Fields)
            Debug.WriteLine(
                $"Hamlet: Field: {headerField.Name}, Type: {headerField.Type.Name}"
            );
#endif

        while (shapefileReader.Read())
        {
            Geometry geometry = shapefileReader.Geometry;
            
            // Process shape
            
            Point centroid = geometry.Centroid;

            (double lon, double lat) = coordTransformation.MathTransform.Transform(centroid.X, centroid.Y);

            OsmCoord coord = new OsmCoord(lat, lon);

            // Process columns
            
            string status = shapefileReader["STATUSS"].ToString() ?? throw new Exception("Hamlet in data without a status");
            string approved = shapefileReader["APSTIPR"].ToString() ?? throw new Exception("Hamlet in data without an approval status");
            string name = shapefileReader["NOSAUKUMS"].ToString() ?? throw new Exception("Hamlet in data without a name");
            string address = shapefileReader["STD"].ToString() ?? throw new Exception("Hamlet in data without a full address");
            string id = shapefileReader["KODS"].ToString() ?? throw new Exception("Hamlet in data without a code");

            bool isValid = status == "EKS" && approved == "Y";

            // Entry
            
            Villages.Add(
                new Village(
                    isValid,
                    id,
                    coord,
                    name,
                    address,
                    true,
                    null // hamlets have no boundaries in data
                )
            );
        }
    }
}