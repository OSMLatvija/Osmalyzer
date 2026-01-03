using System.Diagnostics;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Osmalyzer;

[UsedImplicitly]
public class AddressGeodataAnalysisData : AnalysisData, IUndatedAnalysisData
    // todo: , IDatedAnalysisData - need to parse
{
    public override string Name => "Address Geospatial data";

    public override string ReportWebLink => @"https://data.gov.lv/dati/dataset/varis-atvertie-dati/resource/b643b1b3-223f-4394-9beb-18524f8b0b82";

    public override bool NeedsPreparation => true;


    public List<Village> Villages { get; private set; } = null!; // only null before prepared
    
    public List<Hamlet> Hamlets { get; private set; } = null!; // only null before prepared
    
    public List<Parish> Parishes { get; private set; } = null!; // only null before prepared
    
    public List<Municipality> Municipalities { get; private set; } = null!; // only null before prepared
    
    public List<City> Cities { get; private set; } = null!; // only null before prepared


    protected override string DataFileIdentifier => "addresses-geo";


    private string ExtractionFolder => "ADRGEO";
    
    private HashSet<string>? _duplicateParishNames;
    private HashSet<string>? _duplicateVillageNames;
    private HashSet<string>? _duplicateHamletNames;


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
        ParseParishes();
        ParseMunicipalities();
        ParseCities();
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
            
            // Clean up and normalize values
            
            bool isValid = status == "EKS" && approved == "Y";

            // Parse address to extract parent names
            // Format: "Garciems, Carnikavas pag., Ādažu nov."
            string[] addressParts = address.Split(", ");
            if (addressParts.Length != 3)
                throw new Exception($"Village address '{address}' does not match expected format 'VillageName, ParishName pag., MunicipalityName nov.'");
            
            string parishName = addressParts[1].Replace(" pag.", " pagasts");
            string municipalityName = addressParts[2].Replace(" nov.", " novads");

            // Process boundary

            OsmMultiPolygon boundary = OsmMultiPolygon.FromNTSGeometry(
                geometry,
                (x, y) => coordTransformation.MathTransform.Transform(x, y)
            );
            
            // Entry
           
            Villages.Add(
                new Village(
                    isValid,
                    id,
                    coord,
                    name,
                    address,
                    parishName,
                    municipalityName,
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

        Hamlets = [ ];

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

            // Clean up and normalize values
           
            bool isValid = status == "EKS" && approved == "Y";

            // Parse address to extract parent names
            // Format: "Ķeizarsils, Salaspils pag., Salaspils nov."
            string[] addressParts = address.Split(", ");
            if (addressParts.Length != 3)
                throw new Exception($"Hamlet address '{address}' does not match expected format 'HamletName, ParishName pag., MunicipalityName nov.'");
            
            string parishName = addressParts[1].Replace(" pag.", " pagasts");
            string municipalityName = addressParts[2].Replace(" nov.", " novads");

            // Entry
            
            Hamlets.Add(
                new Hamlet(
                    isValid,
                    id,
                    coord,
                    name,
                    address,
                    parishName,
                    municipalityName
                )
            );
        }
    }

    private void ParseParishes()
    {
        string projectionfilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Pagasti.prj");
        CoordinateSystem ourWkt = new CoordinateSystemFactory().CreateFromWkt(File.ReadAllText(projectionfilePath));
            
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
            
        ICoordinateTransformation coordTransformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(
            ourWkt, 
            wgs84
        );

        string shapefilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Pagasti.shp");

        using ShapefileDataReader shapefileReader = new ShapefileDataReader(shapefilePath, GeometryFactory.Default);

        Parishes = [ ];
            
        while (shapefileReader.Read())
        {
            Geometry geometry = shapefileReader.Geometry;
            
            // Process shape
                
            Point centroid = geometry.Centroid;

            (double lon, double lat) = coordTransformation.MathTransform.Transform(centroid.X, centroid.Y);

            OsmCoord coord = new OsmCoord(lat, lon);
            
            // Process columns
            
            string status = shapefileReader["STATUSS"].ToString() ?? throw new Exception("Parish in data without a status");
            string approved = shapefileReader["APSTIPR"].ToString() ?? throw new Exception("Parish in data without an approval status");
            string name = shapefileReader["NOSAUKUMS"].ToString() ?? throw new Exception("Parish in data without a name");
            string address = shapefileReader["STD"].ToString() ?? throw new Exception("Parish in data without a full address");
            string id = shapefileReader["KODS"].ToString() ?? throw new Exception("Parish in data without a code");
            
            // Clean up and normalize values
            
            bool isValid = status == "EKS" && approved == "Y";
            name = name.Replace(" pag.", " pagasts");

            // Parse address to extract parent names
            // Format: "Brunavas pag., Bauskas nov."
            string[] addressParts = address.Split(", ");
            if (addressParts.Length != 2)
                throw new Exception($"Parish address '{address}' does not match expected format 'ParishName pag., MunicipalityName nov.'");
            
            string municipalityName = addressParts[1].Replace(" nov.", " novads");

            // Process boundary

            OsmMultiPolygon boundary = OsmMultiPolygon.FromNTSGeometry(
                geometry,
                (x, y) => coordTransformation.MathTransform.Transform(x, y)
            );
            
            // Entry
           
            Parishes.Add(
                new Parish(
                    isValid,
                    id,
                    coord,
                    name,
                    address,
                    municipalityName,
                    boundary
                )
            );
        }
    }

    private void ParseMunicipalities()
    {
        string projectionfilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Novadi.prj");
        CoordinateSystem ourWkt = new CoordinateSystemFactory().CreateFromWkt(File.ReadAllText(projectionfilePath));
            
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
            
        ICoordinateTransformation coordTransformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(
            ourWkt, 
            wgs84
        );

        string shapefilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Novadi.shp");

        using ShapefileDataReader shapefileReader = new ShapefileDataReader(shapefilePath, GeometryFactory.Default);

        Municipalities = [ ];
            
        while (shapefileReader.Read())
        {
            Geometry geometry = shapefileReader.Geometry;
            
            // Process shape
                
            Point centroid = geometry.Centroid;

            (double lon, double lat) = coordTransformation.MathTransform.Transform(centroid.X, centroid.Y);

            OsmCoord coord = new OsmCoord(lat, lon);
            
            // Process columns
            
            string status = shapefileReader["STATUSS"].ToString() ?? throw new Exception("Municipality in data without a status");
            string approved = shapefileReader["APSTIPR"].ToString() ?? throw new Exception("Municipality in data without an approval status");
            string name = shapefileReader["NOSAUKUMS"].ToString() ?? throw new Exception("Municipality in data without a name");
            string address = shapefileReader["STD"].ToString() ?? throw new Exception("Municipality in data without a full address");
            string id = shapefileReader["KODS"].ToString() ?? throw new Exception("Municipality in data without a code");
            
            // Clean up and normalize values
            
            bool isValid = status == "EKS" && approved == "Y";
            name = name.Replace(" nov.", " novads");

            // Process boundary

            OsmMultiPolygon boundary = OsmMultiPolygon.FromNTSGeometry(
                geometry,
                (x, y) => coordTransformation.MathTransform.Transform(x, y)
            );
            
            // Entry
           
            Municipalities.Add(
                new Municipality(
                    isValid,
                    id,
                    coord,
                    name,
                    address,
                    boundary
                )
            );
        }
    }

    private void ParseCities()
    {
        string projectionfilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Pilsetas.prj");
        CoordinateSystem ourWkt = new CoordinateSystemFactory().CreateFromWkt(File.ReadAllText(projectionfilePath));
            
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
            
        ICoordinateTransformation coordTransformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(
            ourWkt, 
            wgs84
        );

        string shapefilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "Pilsetas.shp");

        using ShapefileDataReader shapefileReader = new ShapefileDataReader(shapefilePath, GeometryFactory.Default);

        Cities = [ ];
            
        while (shapefileReader.Read())
        {
            Geometry geometry = shapefileReader.Geometry;
            
            // Process shape
                
            Point centroid = geometry.Centroid;

            (double lon, double lat) = coordTransformation.MathTransform.Transform(centroid.X, centroid.Y);

            OsmCoord coord = new OsmCoord(lat, lon);
            
            // Process columns
            
            string status = shapefileReader["STATUSS"].ToString() ?? throw new Exception("City in data without a status");
            string approved = shapefileReader["APSTIPR"].ToString() ?? throw new Exception("City in data without an approval status");
            string name = shapefileReader["NOSAUKUMS"].ToString() ?? throw new Exception("City in data without a name");
            string address = shapefileReader["STD"].ToString() ?? throw new Exception("City in data without a full address");
            string id = shapefileReader["KODS"].ToString() ?? throw new Exception("City in data without a code");
            
            // Clean up and normalize values
            
            bool isValid = status == "EKS" && approved == "Y";

            // Parse address to extract parent names
            // Format: "Rūjiena, Valmieras nov." (regular city)
            // Format: "Jūrmala" (valstpilsēta - city by itself)
            string[] addressParts = address.Split(", ");
            string? municipalityName;
            
            if (addressParts.Length == 1)
            {
                // Valstpilsēta - city by itself, no municipality
                municipalityName = null;
            }
            else if (addressParts.Length == 2)
            {
                // Regular city within a municipality
                municipalityName = addressParts[1].Replace(" nov.", " novads");
            }
            else
            {
                throw new Exception($"City address '{address}' does not match expected format 'CityName' or 'CityName, MunicipalityName nov.'");
            }

            // Process boundary

            OsmMultiPolygon boundary = OsmMultiPolygon.FromNTSGeometry(
                geometry,
                (x, y) => coordTransformation.MathTransform.Transform(x, y)
            );
            
            // Entry
           
            Cities.Add(
                new City(
                    isValid,
                    id,
                    coord,
                    name,
                    address,
                    municipalityName,
                    boundary
                )
            );
        }
    }
    
    [Pure]
    public bool IsUniqueParishName(string name)
    {
        if (_duplicateParishNames == null)
        {
            // Build duplicate name set
            _duplicateParishNames = [ ];

            HashSet<string> seenNames = [ ];

            foreach (Parish parish in Parishes)
                if (!seenNames.Add(parish.Name))
                    _duplicateParishNames.Add(parish.Name);
        }
        
        return !_duplicateParishNames.Contains(name);
    }

    [Pure]
    public bool IsUniqueVillageName(string name)
    {
        if (_duplicateVillageNames == null)
        {
            // Build duplicate name set
            _duplicateVillageNames = [ ];

            HashSet<string> seenNames = [ ];

            foreach (Village village in Villages)
                if (!seenNames.Add(village.Name))
                    _duplicateVillageNames.Add(village.Name);
        }
        
        return !_duplicateVillageNames.Contains(name);
    }

    [Pure]
    public bool IsUniqueHamletName(string name)
    {
        if (_duplicateHamletNames == null)
        {
            // Build duplicate name set
            _duplicateHamletNames = [ ];

            HashSet<string> seenNames = [ ];

            foreach (Hamlet hamlet in Hamlets)
                if (!seenNames.Add(hamlet.Name))
                    _duplicateHamletNames.Add(hamlet.Name);
        }
        
        return !_duplicateHamletNames.Contains(name);
    }
}


