using System.Globalization;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace Osmalyzer;

[UsedImplicitly]
public class VdbAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "VDB Place Names";

    public override string ReportWebLink => @"https://data.gov.lv/dati/dataset/vietvrdu-datubze/resource/bbb31d7d-e514-4d71-a004-dff594c710a2";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "vdb";


    public List<VdbEntry> Entries { get; private set; } = null!; // only null before prepared
    
    public List<RawVdbEntry> RawEntries { get; private set; } = null!; // only null before prepared


    public static readonly string[] FieldNames =
    [
        "OBJECTID",
        "OBJEKTAID",
        "PAMATNOSAUKUMS",
        "PAMATNOSAUKUMS2",
        "STAVOKLIS",
        "ATKKODS",
        "PAGASTS",
        "NOVADS",
        "VEIDS",
        "GEOPLATUMS",
        "GEOGARUMS",
        "OFICIALS_NOSAUKUMS",
        "OFICIALS_AVOTS",
        "NOSAUKUMAID",
        "NOSAUKUMS",
        "IZSKANA",
        "GALVENAIS",
        "SKAUZAMAFORMA",
        "IZRUNA",
        "PARDEVETS",
        "SAKUMALAIKS",
        "BEIGULAIKS",
        "LIETOSANASVIDE",
        "LIETOSANASBIEZUMS",
        "KOMENTARI",
        "KARTESNOS",
        "OFIC_NOS_UN_AVOTS",
        "VISI_NOS",
        "OFICIALS",
        "GEO_GAR",
        "GEO_PLAT",
        "FORMA",
        "FORMASID",
        "DATUMSIZM"
    ];


    private string ExtractionFolder => "VDB";


    protected override void Download()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct reading/scraping
            ReportWebLink,
            true
        );

        // Find the download link - pattern: href="(https://s3.storage.pub.lvdc.gov.lv/lgia-opendata/citi/vdb/CSV_\d{8}.zip)"
        Match urlMatch = Regex.Match(result, @"href=""(https://s3\.storage\.pub\.lvdc\.gov\.lv/lgia-opendata/citi/vdb/CSV_\d{8}\.zip)""");

        if (!urlMatch.Success) throw new Exception("Could not find the download URL for VDB place names CSV file.");

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

        // Find the CSV file in the extracted folder structure
        // Structure: CSV_YYYYMMDD/VDB_YYYYMMDD.csv
        
        string[] subfolders = Directory.GetDirectories(ExtractionFolder);
        
        if (subfolders.Length != 1)
            throw new Exception($"Expected 1 subfolder in VDB extraction, found {subfolders.Length}");

        string csvFolder = subfolders[0];
        string[] csvFiles = Directory.GetFiles(csvFolder, "VDB_*.csv");

        if (csvFiles.Length != 1)
            throw new Exception($"Expected 1 VDB CSV file, found {csvFiles.Length}");

        string csvFilePath = csvFiles[0];

        // Parse the CSV
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // needed to find the encoding
        Encoding encoding = Encoding.GetEncoding(1257); // apparently it's encoded in Windows-1257 for Baltic languages, because it's 1996 apparently 

        using StreamReader reader = new StreamReader(csvFilePath, encoding);
        CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ";",
            Encoding = encoding,
            TrimOptions = TrimOptions.Trim
        };
        using CsvReader csv = new CsvReader(reader, config);
        
        // Verify header

        if (!csv.Read()) throw new Exception("Could not read VDB CSV file.");
        if (!csv.ReadHeader()) throw new Exception("Could not read VDB CSV header.");

        if (csv.HeaderRecord!.Length != FieldNames.Length)
            throw new Exception($"Expected {FieldNames.Length} fields in VDB CSV header, got {csv.HeaderRecord.Length}");
        
        for (int i = 0; i < FieldNames.Length; i++)
            if (csv.HeaderRecord[i] != FieldNames[i])
                throw new Exception($"VDB CSV header field mismatch at index {i}: expected '{FieldNames[i]}', got '{csv.HeaderRecord[i]}'");

        Entries = [ ];
        RawEntries = [ ];

        // Parse data rows
        while (csv.Read())
        {
            if (csv.ColumnCount != FieldNames.Length)
                throw new Exception($"Expected {FieldNames.Length} fields in VDB CSV data row, got {csv.ColumnCount} (record {csv.CurrentIndex})");

            // Get full raw record
            
            RawVdbEntry rawEntry = csv.GetRecord<RawVdbEntry>();
            
            RawEntries.Add(rawEntry);
            
            // Extract all fields

            // todo: only stuff we care about
            
            string? objectId = csv.GetField(0);
            string? objectIdAlt = csv.GetField(1);
            string? mainName = csv.GetField(2);
            string? secondaryMainName = csv.GetField(3); 
            string? status = csv.GetField(4);
            string? atkCode = csv.GetField(5);
            string? parish = csv.GetField(6);
            string? municipality = csv.GetField(7);
            string? type = csv.GetField(8);
            string? geoLatitude = csv.GetField(9); 
            string? geoLongitude = csv.GetField(10);
            string? officialName = csv.GetField(11);
            string? officialSource = csv.GetField(12);
            string? nameId = csv.GetField(13);
            string? name = csv.GetField(14);
            string? pronunciation = csv.GetField(15);
            string? isMain = csv.GetField(16);
            string? declinableForm = csv.GetField(17);
            string? enunciation = csv.GetField(18);
            string? transferred = csv.GetField(19);
            string? startTime = csv.GetField(20);
            string? endTime = csv.GetField(21);
            string? usageArea = csv.GetField(22);
            string? usageFrequency = csv.GetField(23);
            string? comments = csv.GetField(24);
            string? mapName = csv.GetField(25);
            string? officialNameAndSource = csv.GetField(26);
            string? allNames = csv.GetField(27);
            string? official = csv.GetField(28);
            string? geoLongitudeAlt = csv.GetField(29);
            string? geoLatitudeAlt = csv.GetField(30);
            string? form = csv.GetField(31);
            string? formId = csv.GetField(32);
            string? dateModified = csv.GetField(33);

            // Parse coordinates
            
            double? lat = null;
            double? lon = null;

            if (!string.IsNullOrWhiteSpace(geoLatitude) && double.TryParse(geoLatitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedLat))
                lat = parsedLat;

            if (!string.IsNullOrWhiteSpace(geoLongitude) && double.TryParse(geoLongitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedLon))
                lon = parsedLon;
            
            OsmCoord? coord = null;
            
            if (lat.HasValue && lon.HasValue)
                coord = new OsmCoord(lat.Value, lon.Value);
            
            // Make entry

            VdbEntry entry = new VdbEntry(
                objectId,
                objectIdAlt,
                mainName,
                secondaryMainName,
                status,
                atkCode,
                parish,
                municipality,
                type,
                coord,
                officialName,
                officialSource,
                nameId,
                name,
                pronunciation,
                isMain,
                declinableForm,
                enunciation,
                transferred,
                startTime,
                endTime,
                usageArea,
                usageFrequency,
                comments,
                mapName,
                officialNameAndSource,
                allNames,
                official,
                geoLongitudeAlt,
                geoLatitudeAlt,
                form,
                formId,
                dateModified
            );

            Entries.Add(entry);
        }
    }

    
    public void AssignToVillages(List<Village> villages)
    {
        // todo:
    }
}


public class VdbEntry
{
    public string? ObjectId { get; }
    public string? ObjectIdAlt { get; }
    public string? MainName { get; }
    public string? SecondaryMainName { get; }
    public string? Status { get; }
    public string? AtkCode { get; }
    public string? Parish { get; }
    public string? Municipality { get; }
    public string? Type { get; }
    public OsmCoord? Coord { get; }
    public string? OfficialName { get; }
    public string? OfficialSource { get; }
    public string? NameId { get; }
    public string? Name { get; }
    public string? Pronunciation { get; }
    public string? IsMain { get; }
    public string? DeclinableForm { get; }
    public string? Enunciation { get; }
    public string? Transferred { get; }
    public string? StartTime { get; }
    public string? EndTime { get; }
    public string? UsageArea { get; }
    public string? UsageFrequency { get; }
    public string? Comments { get; }
    public string? MapName { get; }
    public string? OfficialNameAndSource { get; }
    public string? AllNames { get; }
    public string? Official { get; }
    public string? GeoLongitudeAlt { get; }
    public string? GeoLatitudeAlt { get; }
    public string? Form { get; }
    public string? FormId { get; }
    public string? DateModified { get; }

    public VdbEntry(
        string? objectId,
        string? objectIdAlt,
        string? mainName,
        string? secondaryMainName,
        string? status,
        string? atkCode,
        string? parish,
        string? municipality,
        string? type,
        OsmCoord? coord,
        string? officialName,
        string? officialSource,
        string? nameId,
        string? name,
        string? pronunciation,
        string? isMain,
        string? declinableForm,
        string? enunciation,
        string? transferred,
        string? startTime,
        string? endTime,
        string? usageArea,
        string? usageFrequency,
        string? comments,
        string? mapName,
        string? officialNameAndSource,
        string? allNames,
        string? official,
        string? geoLongitudeAlt,
        string? geoLatitudeAlt,
        string? form,
        string? formId,
        string? dateModified)
    {
        ObjectId = objectId;
        ObjectIdAlt = objectIdAlt;
        MainName = mainName;
        SecondaryMainName = secondaryMainName;
        Status = status;
        AtkCode = atkCode;
        Parish = parish;
        Municipality = municipality;
        Type = type;
        Coord = coord;
        OfficialName = officialName;
        OfficialSource = officialSource;
        NameId = nameId;
        Name = name;
        Pronunciation = pronunciation;
        IsMain = isMain;
        DeclinableForm = declinableForm;
        Enunciation = enunciation;
        Transferred = transferred;
        StartTime = startTime;
        EndTime = endTime;
        UsageArea = usageArea;
        UsageFrequency = usageFrequency;
        Comments = comments;
        MapName = mapName;
        OfficialNameAndSource = officialNameAndSource;
        AllNames = allNames;
        Official = official;
        GeoLongitudeAlt = geoLongitudeAlt;
        GeoLatitudeAlt = geoLatitudeAlt;
        Form = form;
        FormId = formId;
        DateModified = dateModified;
    }
}

public class RawVdbEntry
{
    [Index(0)] [UsedImplicitly] public string? ObjectId { get; set; }
    [Index(1)] [UsedImplicitly] public string? ObjectIdAlt { get; set; }
    [Index(2)] [UsedImplicitly] public string? MainName { get; set; }
    [Index(3)] [UsedImplicitly] public string? SecondaryMainName { get; set; } 
    [Index(4)] [UsedImplicitly] public string? Status { get; set; }
    [Index(5)] [UsedImplicitly] public string? AtkCode { get; set; }
    [Index(6)] [UsedImplicitly] public string? Parish { get; set; }
    [Index(7)] [UsedImplicitly] public string? Municipality { get; set; }
    [Index(8)] [UsedImplicitly] public string? Type { get; set; }
    [Index(9)] [UsedImplicitly] public string? GeoLatitude { get; set; } 
    [Index(10)] [UsedImplicitly] public string? GeoLongitude { get; set; }
    [Index(11)] [UsedImplicitly] public string? OfficialName { get; set; }
    [Index(12)] [UsedImplicitly] public string? OfficialSource { get; set; }
    [Index(13)] [UsedImplicitly] public string? NameId { get; set; }
    [Index(14)] [UsedImplicitly] public string? Name { get; set; }
    [Index(15)] [UsedImplicitly] public string? Pronunciation { get; set; }
    [Index(16)] [UsedImplicitly] public string? IsMain { get; set; }
    [Index(17)] [UsedImplicitly] public string? DeclinableForm { get; set; }
    [Index(18)] [UsedImplicitly] public string? Enunciation { get; set; }
    [Index(19)] [UsedImplicitly] public string? Transferred { get; set; }
    [Index(20)] [UsedImplicitly] public string? StartTime { get; set; }
    [Index(21)] [UsedImplicitly] public string? EndTime { get; set; }
    [Index(22)] [UsedImplicitly] public string? UsageArea { get; set; }
    [Index(23)] [UsedImplicitly] public string? UsageFrequency { get; set; }
    [Index(24)] [UsedImplicitly] public string? Comments { get; set; }
    [Index(25)] [UsedImplicitly] public string? MapName { get; set; }
    [Index(26)] [UsedImplicitly] public string? OfficialNameAndSource { get; set; }
    [Index(27)] [UsedImplicitly] public string? AllNames { get; set; }
    [Index(28)] [UsedImplicitly] public string? Official { get; set; }
    [Index(29)] [UsedImplicitly] public string? GeoLongitudeAlt { get; set; }
    [Index(30)] [UsedImplicitly] public string? GeoLatitudeAlt { get; set; }
    [Index(31)] [UsedImplicitly] public string? Form { get; set; }
    [Index(32)] [UsedImplicitly] public string? FormId { get; set; }
    [Index(33)] [UsedImplicitly] public string? DateModified { get; set; }

    
    // Cached property get methods for each field
    private static Dictionary<int, MethodInfo>? _fieldGetters;
    
    
    public string? GetValue(int fieldIndex)
    {
        if (_fieldGetters == null)
        {
            _fieldGetters = [ ];

            foreach (PropertyInfo property in typeof(RawVdbEntry).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                IndexAttribute indexAttr = property.GetCustomAttribute<IndexAttribute>() ?? throw new Exception("RawVdbEntry property missing Index attribute: " + property.Name);
                _fieldGetters[indexAttr.Index] = property.GetGetMethod(true)!;
            }
        }
        
        return (string?)_fieldGetters[fieldIndex].Invoke(this, null);
    }
}