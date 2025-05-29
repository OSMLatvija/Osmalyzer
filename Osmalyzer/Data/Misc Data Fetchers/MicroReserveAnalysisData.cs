using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace Osmalyzer;

[UsedImplicitly]
public class MicroReserveAnalysisData : AnalysisData, IDatedAnalysisData
{
    public override string Name => "Micro Reserves";

    public override string ReportWebLink => @"https://data.gov.lv/dati/lv/dataset/mikroliegumi";

    public override bool NeedsPreparation => true;


    public bool DataDateHasDayGranularity => false; // only day given on data page

    protected override string DataFileIdentifier => "micro-reserves";

    
    public List<Microreserve> Reserves { get; private set; } = null!; // only null before prepared


    private string ExtractionFolder => "MR";


    public DateTime RetrieveDataDate()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct download/scraping
            "https://data.gov.lv/dati/lv/dataset/mikroliegumi", 
            true
        );

        Match dateMatch = Regex.Match(result, @"Datu pēdējo izmaiņu datums</th>\s*<td class=""dataset-details"">\s*(\d{4})-(\d{2})-(\d{2})");
        int newestYear = int.Parse(dateMatch.Groups[1].ToString());
        int newestMonth = int.Parse(dateMatch.Groups[2].ToString());
        int newestDay = int.Parse(dateMatch.Groups[3].ToString());
                
        return new DateTime(newestYear, newestMonth, newestDay);
    }

    protected override void Download()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct download/scraping
            "https://data.gov.lv/dati/lv/dataset/mikroliegumi", 
            true
        );

        Match urlMatch = Regex.Match(result, @"<a class=""heading"" href=""(/dati/lv/dataset/mikroliegumi/resource/[^""]+)"" title=""mikroliegumi"">");

        string url = @"https://data.gov.lv" + urlMatch.Groups[1];

        result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct download/scraping
            url, 
            true
        );

        urlMatch = Regex.Match(result, @"URL: <a href=""([^""]+)""");

        url = urlMatch.Groups[1].ToString();

        WebsiteDownloadHelper.Download(
            url,
            Path.Combine(CacheBasePath, @"micro-reserves.zip")
        );
    }

    protected override void DoPrepare()
    {
        // Data comes in a zip file, so unzip
            
        ZipHelper.ExtractZipFile(
            Path.Combine(CacheBasePath, DataFileIdentifier + @".zip"),
            Path.GetFullPath(ExtractionFolder)
        );
        
        // Make sure the files are in root and not in subfolder, as the data start being suddenly (e.g. "Mikroliegumi_20.12.2023")
        
        string[] subfolders = Directory.GetDirectories(ExtractionFolder);
        
        if (subfolders.Length == 1)
        {
            string[] subfolderFiles = Directory.GetFiles(subfolders[0]);
            
            foreach (string file in subfolderFiles)
            {
                string destination = Path.Combine(ExtractionFolder, Path.GetFileName(file));
                File.Move(file, destination);
            }

            Directory.Delete(subfolders[0]);
        }
        
        // Parse
        
        string projectionfilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "GIS_OZOLS_Microreserves_PUB.prj");
        CoordinateSystem ourWkt = new CoordinateSystemFactory().CreateFromWkt(File.ReadAllText(projectionfilePath));
            
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
            
        ICoordinateTransformation coordTransformation = new CoordinateTransformationFactory().CreateFromCoordinateSystems(
            ourWkt, 
            wgs84
        );

        string shapefilePath = Path.Combine(Path.GetFullPath(ExtractionFolder), "GIS_OZOLS_Microreserves_PUB.shp");

        using ShapefileDataReader shapefileReader = new ShapefileDataReader(shapefilePath, GeometryFactory.Default);

        DbaseFileHeader dbaseHeader = shapefileReader.DbaseHeader;

#if !REMOTE_EXECUTION
        // Dump header info
            
        StreamWriter dumpFileWriter = File.CreateText(DataFileIdentifier + "_shp_header_dump.tsv");
            
        dumpFileWriter.WriteLine("Rows: Field name (shapefile); Field type (shapefile); Field name (XML); Field label (XML); Shapefile rows...");

        dumpFileWriter.WriteLine(string.Join("\t", dbaseHeader.Fields.Select(f => f.Name)));
            
        dumpFileWriter.WriteLine(string.Join("\t", dbaseHeader.Fields.Select(f => f.Type.Name)));
            
        string xmlPath = Path.Combine(Path.GetFullPath(ExtractionFolder), "GIS_OZOLS_Microreserves_PUB.shp.xml");
        MatchCollection fieldDescMatches = Regex.Matches(File.ReadAllText(xmlPath), @"<attrlabl Sync=""TRUE"">([^<]+)</attrlabl><attalias Sync=""TRUE"">([^<]+)</attalias>");

        dumpFileWriter.WriteLine(string.Join("\t", dbaseHeader.Fields.Select(f =>
        {
            Match? match = fieldDescMatches.FirstOrDefault(m => m.Groups[1].ToString().StartsWith(f.Name));
            return match != null ? match.Groups[1].ToString() : "UNMATCHED";
        })));
        dumpFileWriter.WriteLine(string.Join("\t", dbaseHeader.Fields.Select(f =>
        {
            Match? match = fieldDescMatches.FirstOrDefault(m => m.Groups[1].ToString().StartsWith(f.Name));
            return match != null ? match.Groups[2].ToString() : "UNMATCHED";
        })));
        // Note the match check and StartsWith because the shapefile data header names are both wrong and mismatched
        // The reader doesn't have this meta-info or at least I didn't find it, so I just manually grab it from XML
#endif

        // Read shapes
            
        while (shapefileReader.Read())
        {
            Geometry geometry = shapefileReader.Geometry;

#if !REMOTE_EXECUTION
            // Dump all non-shape fields
                
            object?[] values = new object?[dbaseHeader.Fields.Length];
                
            for (int i = 0; i < dbaseHeader.Fields.Length; i++)
                values[i] = shapefileReader.GetValue(i + 1); // 0 is apparently the shape itself and columns start with 1 - this doesn't exactly match XML 

            int mrObject = (int)(double)values[1]!;
            int mrType = (int)(double)values[8]!;
                
            dumpFileWriter.WriteLine(
                string.Join("\t", values.Select(v => v != null ? v.ToString() : "NULL")) +
                "\t" + MakeProtectionObjectValue(mrObject, mrType)
            );
                
            [Pure]
            static string MakeProtectionObjectValue(int mrObject, int mrType)
            {
                switch (mrObject)
                {
                    case 0: // Nav definēts
                        return MakeTypeValue(mrType); 
                    // There is exactly 1 entry 0-4 - "birds" type but undefined object
                            
                    case 1: // Biotops un suga
                        if (mrType == 10)
                            return "biotope";
                                
                        return "biotope; " + MakeTypeValue(mrType);
                            
                    case 2: // Biotops
                        if (mrType != 10) throw new NotImplementedException();
                        // All entries are 2-10
                            
                        return "biotope";
                            
                    case 3: // Suga
                        return MakeTypeValue(mrType);
                        
                    default: 
                        throw new NotImplementedException();
                }

                [Pure]
                static string MakeTypeValue(int mrType)
                {
                    switch (mrType)
                    {
                        case 0:  return "amphibians"; // Abinieki
                        case 1:  return "reptiles"; // Rāpuļi
                        case 2:  return "invertebrates"; // Bezmugurkaulnieki
                        case 3:  return "fish"; // Zivis
                        case 4:  return "birds"; // Putni
                        case 5:  return "mammals"; // Zīdītāji
                        case 6:  return "vascular plants and ferns"; // Vaskulārie augi un paparžaugi
                        case 7:  return "lichens"; // Ķērpji
                        case 8:  return "moss"; // Sūnas
                        case 9:  return "mushrooms"; // Sēnes
                        case 10: return "biotope"; // Biotopi
                            
                        default: throw new NotImplementedException();
                    }
                }
            }
#endif

            // Process shape
                
            Point centroid = geometry.Centroid;

            (double lon, double lat) = coordTransformation.MathTransform.Transform(centroid.X, centroid.Y);

            OsmCoord coord = new OsmCoord(lat, lon);
                
            Reserves.Add(
                new Microreserve(
                    coord,
                    geometry.Area
                )
            );
        }

#if !REMOTE_EXECUTION
        dumpFileWriter.Close();
#endif
    }
}