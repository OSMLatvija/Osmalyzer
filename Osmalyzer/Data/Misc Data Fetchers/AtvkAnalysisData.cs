using System.Globalization;

namespace Osmalyzer;

[UsedImplicitly]
public class AtvkAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "ATVK units";

    public override string ReportWebLink => @"https://data.gov.lv/dati/lv/dataset/atvk/resource/6d8624c4-e75a-4080-88eb-c755b5de230a";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "atvk";


    public List<AtvkEntry> Entries { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct reading/scraping
            ReportWebLink,
            true
        );

        Match urlMatch = Regex.Match(result, @"href=""(https://data\.gov\.lv/dati/dataset/[^/]*/resource/[^/]*/download/atu_nuts_codes\.csv)""");

        if (!urlMatch.Success) throw new Exception("Could not find the download URL for ATVK (ATU NUTS) codes CSV file.");

        string url = urlMatch.Groups[1].ToString();

        WebsiteBrowsingHelper.DownloadPage( // data.gov.lv seems to not like direct download/scraping
            url,
            Path.Combine(CacheBasePath, DataFileIdentifier + @".csv")
        );
    }

    protected override void DoPrepare()
    {
        Entries = [ ];

        string dataFileName = Path.Combine(CacheBasePath, DataFileIdentifier + @".csv");

        string[] lines = File.ReadAllLines(dataFileName, Encoding.UTF8);

        // First pass: Parse all entries
        for (int i = 1; i < lines.Length; i++) // Skip header line
        {
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Parse CSV line (handling quoted fields properly)
            // Fields: Level,Code,Code_version,Name,Code_parent,Validity_period_begin,Validity_period_end,Predecessors,Successors
            string[] fields = CsvParser.ParseLine(line, ',');

            if (fields.Length < 9)
                throw new Exception($"Expected at least 9 fields in CSV line, got {fields.Length}: {line}");

            // Parse fields
            string level = fields[0];
            string code = fields[1];
            // int codeVersion = int.Parse(fields[2]); // we don't need code version
            string name = fields[3];
            string? codeParent = string.IsNullOrWhiteSpace(fields[4]) ? null : fields[4];
            
            DateTime validityBegin = DateTime.Parse(fields[5], CultureInfo.InvariantCulture);
            
            DateTime? validityEnd = null;
            if (!string.IsNullOrWhiteSpace(fields[6]))
                validityEnd = DateTime.Parse(fields[6], CultureInfo.InvariantCulture);

            // We ignore predecessors (field[7]) and successors (field[8])

            // Convert level to enum
            AtvkLevel atvkLevel = validityEnd == null ? int.Parse(level) switch
            {
                // 0 is highest
                // Latvija
                0 => AtvkLevel.Country,
                    
                // Latgale, Zemgale, Rīga, Kurzeme, Vidzeme
                1 => AtvkLevel.Region,
                
                // There is no 2 (in active)
                
                // Rīga, Daugavpils, Jelgava, ... Aizkraukles novads, Augšdaugavas novads, Balvu novads, ...
                3 => AtvkLevel.CityOrMunicipality,
                
                // Aizkraukle, Jaunjelgava, Koknese, ... Aiviekstes pagasts, Aizkraukles pagasts, Bebru pagasts...
                4 => AtvkLevel.CityOrParish,
                // 4 is lowest
                
                _ => throw new Exception($"Unknown ATVK level value: {level}")
            } : AtvkLevel.Expired;

            AtvkDesignation designation = validityEnd == null ? atvkLevel switch
            {
                AtvkLevel.Country            => AtvkDesignation.Country,
                AtvkLevel.Region             => AtvkDesignation.Region,
                AtvkLevel.CityOrMunicipality => name.EndsWith(" novads") ? AtvkDesignation.Municipality : AtvkDesignation.CityInRegion,
                AtvkLevel.CityOrParish       => name.EndsWith(" pagasts") ? AtvkDesignation.Parish : AtvkDesignation.CityInMunicipality,
                _                            => throw new Exception()
            } : AtvkDesignation.Expired;

            AtvkEntry entry = new AtvkEntry(
                code,
                name,
                atvkLevel,
                designation,
                codeParent,
                validityBegin,
                validityEnd
            );

            // if (validityEnd == null)
            //     Console.WriteLine(entry.ReportString());
            
            Entries.Add(
                entry
            );
        }

        // Second pass: Link parents for active entries only
        // Assumption: codes are unique among active entries
        Dictionary<string, AtvkEntry> activeEntryByCode = Entries
            .Where(e => !e.IsExpired)
            .ToDictionary(e => e.Code);

        foreach (AtvkEntry entry in Entries.Where(e => !e.IsExpired))
        {
            if (entry.CodeParent != null && activeEntryByCode.TryGetValue(entry.CodeParent, out AtvkEntry? parent))
            {
                entry.Parent = parent;
                
                parent.Children ??= [ ];
                parent.Children.Add(entry);
            }
        }

#if DEBUG
        // // Print out state (Latgale, Zemgale, Kurzeme, Vidzeme) children
        // foreach (AtvkEntry region in Entries.Where(e => !e.IsExpired && e.Level == AtvkLevel.Region))
        // {
        //     Console.WriteLine($"Region {region.Name} has children:");
        //     if (region.Children == null) throw new Exception("Region has no children list despite being non-expired.");
        //     
        //     foreach (AtvkEntry child in region.Children)
        //         Console.WriteLine($"  - {child.ReportString()}");
        // }
#endif
    }


    /// <summary>
    /// Assigns matching ATVK entries to data items
    /// </summary>
    public void AssignToDataItems<T>(
        List<T> dataItems,
        List<AtvkEntry> atvkEntries,
        Func<T, AtvkEntry, bool> matcher)
        where T : class, IDataItem, IHasAtvkEntry
    {
        int count = 0;

        foreach (T dataItem in dataItems)
        {
            foreach (AtvkEntry atvkEntry in atvkEntries)
            {
                if (matcher(dataItem, atvkEntry))
                {
                    dataItem.AtvkEntry = atvkEntry;
                    count++;
                    break;
                }
            }
        }

        if (count == 0)
            throw new Exception("No ATVK matches found for data items; data is probably broken.");
    }
}


