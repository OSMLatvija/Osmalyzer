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
    
    public List<VdbEntry> AdminEntries { get; private set; } = null!; // only null before prepared
    
    public List<RawVdbEntry> RawEntries { get; private set; } = null!; // only null before prepared


    public static readonly string[] FieldNames =
    [
        "OBJECTID", // row number basically, useless
        "OBJEKTAID", // the actual unqiue ID of the VDB, presumably
        "PAMATNOSAUKUMS", // primary name of the feature, all have it
        "PAMATNOSAUKUMS2", // alternative name of the feature, 99.4% don't have it
        "STAVOKLIS", // state of the feature, 96% exist, all have it
        "ATKKODS", // TODO: don't know what this is
        "PAGASTS", // as expected "Naujenes pagasts", but also stuff like "Latvija", "Rīga", "Ogres novads"
        "NOVADS", // mostly as expected "Augšdaugavas novads", but also "Latvija" and "Eiropas Savienība"
        "VEIDS", // type of feature, all have it, seems strictly-defined
        "GEOPLATUMS", // decimal latitude in WGS84, all have it
        "GEOGARUMS", // decimal longitude in WGS84, all have it
        "OFICIALS_NOSAUKUMS", // official name from some source, like addresses, but this can be multiname like "Mārupes novads, Mārupes pagasts" or "Kašatniki, Kašatniki"
        "OFICIALS_AVOTS", // the source of the official name
        "NOSAUKUMAID", // presumably the id within the source
        "NOSAUKUMS", // todo: not sure what this is
        "IZSKANA", // related to pronunciation
        "GALVENAIS", // todo: no idea, values are mostly 97% "1", 12% "0" and 0.4% "1" 
        "SKAUZAMAFORMA", // todo: not sure, declinable form? 99.6% are "0" and rest are empty, related to pronunciation?
        "IZRUNA", // related to pronunciation
        "PARDEVETS", // todo: not sure, I would guess old name, but 99.975% of entries don't have it
        "SAKUMALAIKS", // presumably incomplete, 99.6% don't have it and values are freeform like "muižas laiks", "2012.g." etc.
        "BEIGULAIKS", // about the same as above
        "LIETOSANASVIDE", // free-form way of use like "vietējie iedzīvotāji", but 99.5% don't have it
        "LIETOSANASBIEZUMS", // related to above presumably, mostly empty
        "KOMENTARI", // some unparsable internal comments
        "KARTESNOS", // whether it should appear on maps, 87% is "1", rest "0"
        "OFIC_NOS_UN_AVOTS", // seems like combination of official name and source like "Kalniņi [AR]" for official name "Kalniņi" and source "AR", presumably for website
        "VISI_NOS", // some kind of combined names field, presumably for website
        "OFICIALS", // two values "Oficiāls" fo 60% and "Neoficiāls" for 40%, but there seem to be errors
        "GEO_GAR", // presumably parsed from GEOGARUMS to readable "26° 22' 21""
        "GEO_PLAT", // presumably parsed from GEOPLATUMS to readable "56° 33' 56""
        "FORMA", // seems to describe some special flag like "literārā forma", "kļūdaina forma", etc. but 99% don't have it
        "FORMASID", // todo: no idea, it has numeric values from 1 to 11, 99.2% are "6" - probably some internal code
        "DATUMSIZM" // last change date like "02.02.2004 00:00", presumably internal changes
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
            HasHeaderRecord = true, // first row is (known) header
            Delimiter = ";", // it's semicolon-separated
            TrimOptions = TrimOptions.Trim,
            LineBreakInQuotedFieldIsBadData = false // it's found a lot in comments / KOMENTARI
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
        AdminEntries = [ ];

        HashSet<string> objectIds = [ ];

        // Parse data rows
        
        while (csv.Read())
        {
            if (csv.ColumnCount != FieldNames.Length)
                throw new Exception($"Expected {FieldNames.Length} fields in VDB CSV data row, got {csv.ColumnCount} (record {csv.CurrentIndex})");

            // Get full raw record
            
            RawVdbEntry rawEntry = csv.GetRecord<RawVdbEntry>();
            
            // Validate fields
            
            if (rawEntry.ObjectId == null) 
                throw new Exception("Missing OBJECTID in VDB CSV data (record " + csv.CurrentIndex + ")");
            
            if (!objectIds.Add(rawEntry.ObjectId))
                throw new Exception("Duplicate OBJECTID found in VDB CSV data: " + rawEntry.ObjectId);
            
            // Looks good
            RawEntries.Add(rawEntry);
            
            // Parse fields

            long id = long.Parse(rawEntry.ObjectId);

            // All entries have coordinates specified
            double latitude = double.Parse(rawEntry.GeoLatitude!, CultureInfo.InvariantCulture);
            double longitude = double.Parse(rawEntry.GeoLongitude!, CultureInfo.InvariantCulture);
            OsmCoord coord = new OsmCoord(latitude, longitude);
            
            // All entries have main name specified and some have alt name
            string name = rawEntry.MainName?.Trim() ?? throw new Exception("Missing PAMATNOSAUKUMS in VDB CSV data (record " + csv.CurrentIndex + ")");
            string? altName = rawEntry.SecondaryMainName?.Trim();
            string? officialName = rawEntry.OfficialName?.Trim();
            
            if (altName == "") altName = null;
            if (officialName == "") officialName = null;
            
            // All entries have one of the pre-defined states specified
            VdbEntryState state = rawEntry.State switch
            {
                "pastāv"               => VdbEntryState.Exists,
                "daļēji izzudis"       => VdbEntryState.PartiallyGone,
                "nepastāv"             => VdbEntryState.Gone,
                "nedarbojas"           => VdbEntryState.NotOperating,
                "nezināms"             => VdbEntryState.Unknown,
                "nosusināts/ nolaists" => VdbEntryState.Drained,
                null                   => throw new Exception("Missing STAVOKLIS in VDB CSV data (record " + csv.CurrentIndex + ")"),
                _                      => throw new Exception("Unknown STAVOKLIS value in VDB CSV data: " + rawEntry.State + " (record " + csv.CurrentIndex + ")")
            };
            
            // All entries have parish and municipality specified
            string location1 = rawEntry.Parish?.Trim() ?? throw new Exception("Missing PAGASTS in VDB CSV data (record " + csv.CurrentIndex + ")");
            string location2 = rawEntry.Municipality?.Trim() ?? throw new Exception("Missing NOVADS in VDB CSV data (record " + csv.CurrentIndex + ")");
            
            // There are a lot of types, so process the ones of immediate interest 

            VdbEntryObjectType objectType = rawEntry.Type switch
            {
                "viensēta" => VdbEntryObjectType.Hamlet,

                // Admin divisions
                "ciems"          => VdbEntryObjectType.Village,
                "mazciems"       => VdbEntryObjectType.Hamlet,
                "pagasts"        => VdbEntryObjectType.Parish,
                "novads"         => VdbEntryObjectType.Municipality,
                "valstspilsēta"  => VdbEntryObjectType.StateCity,
                "novada pilsēta" => VdbEntryObjectType.MunicipalCities,
                // These seem to line up with VZD and admin law divisions
                // Note that "novada pašvaldība" and "valstspilsētas pašvaldība" means "the office" location not some division

                _ => VdbEntryObjectType.Unparsed
            };
            
            bool official = rawEntry.Official switch
            {
                "Oficiāls"   => true,
                "Neoficiāls" => false,
                null         => throw new Exception("Missing OFICIALS in VDB CSV data (record " + csv.CurrentIndex + ")"),
                _            => throw new Exception("Unknown OFICIALS value in VDB CSV data: " + rawEntry.Official + " (record " + csv.CurrentIndex + ")")
            };
            
            // Known bad entries
            
            // ID      In data   On website
            // 19276   Sabile    Sabile
            // 79612   Sabile    Sabiles novads
            // 64246   Rēzekne   Rēzekne
            // 119034  Rēzekne   Latgale
            // 28278   Jelgava   Jelgava
            // 119032  Jelgava   Zemgale
            // 16034   Kuldīga   Kuldīga
            // 119030  Kuldīga   Kurzeme
            // 29779   Rīga      Rīga
            // 104609  Rīga      Latvijas Republika
            // 42170   Valmiera  Valmiera
            // 119031  Valmiera  Vidzeme
            // 50652   Aknīste   Aknīste
            // 119033  Aknīste   Sēlija
            // like wtf...
            
            if (id == 79612 && name == "Sabile") continue;
            if (id == 119034 && name == "Rēzekne") continue;
            if (id == 119032 && name == "Jelgava") continue;
            if (id == 119030 && name == "Kuldīga") continue;
            if (id == 104609 && name == "Rīga") continue;
            if (id == 119031 && name == "Valmiera") continue;
            if (id == 119033 && name == "Aknīste") continue;
            
            // Make entry

            VdbEntry entry = new VdbEntry(
                id,
                coord,
                objectType,
                official,
                name,
                altName,
                officialName,
                state,
                location1,
                location2
            );

            switch (objectType)
            {
                case VdbEntryObjectType.Village:
                case VdbEntryObjectType.Hamlet:
                case VdbEntryObjectType.Parish:
                case VdbEntryObjectType.Municipality:
                case VdbEntryObjectType.StateCity:
                case VdbEntryObjectType.MunicipalCities:
                    AdminEntries.Add(entry);
                    break;
            }

            Entries.Add(entry);
        }
        
        if (RawEntries.Count == 0)
            throw new Exception("No entries found in VDB CSV data.");
        
#if DEBUG
        // Resave properly formatted CSV for easier debugging
        using StreamWriter debugWriter = new StreamWriter("vdb_debug.csv", false, Encoding.UTF8);
        using CsvWriter debugCsv = new CsvWriter(debugWriter, CultureInfo.InvariantCulture);
        debugCsv.WriteHeader<RawVdbEntry>();
        debugCsv.NextRecord();
        foreach (RawVdbEntry rawEntry in RawEntries)
        {
            debugCsv.WriteRecord(rawEntry);
            debugCsv.NextRecord();
        }
#endif    
        
#if DEBUG
        // Find entries with all fields exactly the same except row and main ID and modified date
        // This is how I first cities broken, so just mass-check it all for the same symptom
        
        Dictionary<string, List<RawVdbEntry>> duplicateCandidates = [ ];
        
        foreach (RawVdbEntry rawEntry in RawEntries)
        {
            // Create a key based on all fields except OBJECTID, OBJEKTAID and DATUMSIZM
            List<string?> keyFields = [ ];
            
            for (int i = 0; i < FieldNames.Length; i++)
            {
                if (FieldNames[i] == "OBJECTID" || FieldNames[i] == "OBJEKTAID" || FieldNames[i] == "DATUMSIZM")
                    continue;
                
                keyFields.Add(rawEntry.GetValue(i));
            }

            string key = string.Join("|", keyFields);
            
            if (!duplicateCandidates.ContainsKey(key))
                duplicateCandidates[key] = [ ];
            
            duplicateCandidates[key].Add(rawEntry);
        }

        using StreamWriter debugWriter2 = new StreamWriter("vdb_broken_dupes.csv", false, Encoding.UTF8);
        using CsvWriter debugCsv2 = new CsvWriter(debugWriter2, CultureInfo.InvariantCulture);
        debugCsv2.WriteHeader<RawVdbEntry>();
        debugCsv2.NextRecord();
        foreach (KeyValuePair<string, List<RawVdbEntry>> dupeList in duplicateCandidates.Where(kvp => kvp.Value.Count > 1))
        {
            foreach (RawVdbEntry rawEntry in dupeList.Value)
            {
                debugCsv2.WriteRecord(rawEntry);
                debugCsv2.NextRecord();
            }
        }
#endif        
    }

    
    /// <summary>
    /// Assigns VDB entries to data items by matching with name and location
    /// </summary>
    public void AssignToDataItems<T>(
        List<T> dataItems,
        Func<T, VdbEntry, bool> matcher,
        double coordMismatchDistance,
        out List<VdbMatchIssue> issues)
        where T : class, IDataItem, IHasVdbEntry
    {
        issues = [ ];
        
        int count = 0;
        
        foreach (T dataItem in dataItems)
        {
            List<VdbEntry> matches = Entries.Where(vdb => matcher(dataItem, vdb)).ToList();
           
            if (matches.Count == 0)
                continue;
            
            if (matches.Count > 1)
            {
                issues.Add(new MultipleVdbMatchesVdbMatchIssue<T>(dataItem, matches));
                
                continue;
            }

            double distance = OsmGeoTools.DistanceBetweenCheap(dataItem.Coord, matches[0].Coord);
            
            if (distance > coordMismatchDistance)
            {
                issues.Add(new CoordinateMismatchVdbMatchIssue<T>(dataItem, matches[0], distance));
                continue;
            }

            dataItem.VdbEntry = matches[0];
            count++;
        }
        
        if (count == 0) throw new Exception("No VDB entries were matched, which is unexpected and likely means data or logic is broken.");
    }
}


public abstract record VdbMatchIssue;

public record MultipleVdbMatchesVdbMatchIssue<T>(T DataItem, List<VdbEntry> VdbEntries) : VdbMatchIssue;

public record CoordinateMismatchVdbMatchIssue<T>(T DataItem, VdbEntry VdbEntry, double DistanceMeters) : VdbMatchIssue;


public class VdbEntry : IDataItem
{
    public long ID { get; }
    
    public OsmCoord Coord { get; }
    
    public VdbEntryObjectType ObjectType { get; }
    
    public bool Official { get; }

    public string Name { get; }
    
    public string? AltName { get; }
    
    public string? OfficialName { get; }

    /// <summary> Strongly-typed known feature type, if implemented </summary>
    public VdbEntryState State { get; }
    
    /// <summary> Usually parish, but can also be city, country or municipality </summary>
    public string Location1 { get; }
    
    /// <summary> Usually municipality, but can also be "Latvija" and "Eiropas Savienība" </summary>
    public string Location2 { get; }


    public bool IsActive => State == VdbEntryState.Exists; // not including PartiallyGone to be more strict


    public VdbEntry(long id, OsmCoord coord, VdbEntryObjectType objectType, bool official, string name, string? altName, string? officialName, VdbEntryState state, string location1, string location2)
    {
        if (name == "") throw new ArgumentException("VDB entry name cannot be empty string.");
        if (altName == "") throw new ArgumentException("VDB entry alt name cannot be empty string.");
        if (officialName == "") throw new ArgumentException("VDB entry official name cannot be empty string.");
        if (location1 == "") throw new ArgumentException("VDB entry location1 cannot be empty string.");
        if (location2 == "") throw new ArgumentException("VDB entry location2 cannot be empty string.");
        
        ID = id;
        Coord = coord;
        ObjectType = objectType;
        Official = official;
        Name = name;
        AltName = altName;
        OfficialName = officialName;
        State = state;
        Location1 = location1;
        Location2 = location2;
    }

    public string ReportString()
    {
        return
            ObjectType +
            (!Official ? " (Unofficial)" : "") +
            (State != VdbEntryState.Exists ? " [" + State + "]" : "") +
            " `" + Name + "`" +
            (AltName != null ? " / `" + AltName + "` (A)" : "") +
            (OfficialName != null && Name != OfficialName ? " / `" + OfficialName + "` (O)" : "") +
            " #" + ID +
            " in `" + Location1 + "`, `" + Location2 + "`" +
            " at " + Coord.OsmUrl;
    }
}

public enum VdbEntryState
{
    Exists,
    PartiallyGone,
    Gone,
    NotOperating,
    Unknown,
    Drained
}

public enum VdbEntryObjectType
{
    /// <summary> Not yet parsed into a specific type </summary>
    Unparsed = -1,
    
    Hamlet,
    Village,
    Parish,
    MunicipalCities,
    Municipality,
    StateCity
}


public class RawVdbEntry
{
    [Index(0)] [UsedImplicitly] public string? EntryID { get; set; }
    [Index(1)] [UsedImplicitly] public string? ObjectId { get; set; }
    [Index(2)] [UsedImplicitly] public string? MainName { get; set; }
    [Index(3)] [UsedImplicitly] public string? SecondaryMainName { get; set; } 
    [Index(4)] [UsedImplicitly] public string? State { get; set; }
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


public interface IHasVdbEntry
{
    VdbEntry? VdbEntry { get; set; }
}