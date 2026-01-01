using System.Globalization;

namespace Osmalyzer;

[UsedImplicitly]
public class AtvkAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "ATVK units";

    public override string ReportWebLink => @"https://data.gov.lv/dati/lv/dataset/atvk/resource/6d8624c4-e75a-4080-88eb-c755b5de230a";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "atvk";


    public List<AtkvEntry> Entries { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct reading/scraping
            ReportWebLink,
            true
        );

        Match urlMatch = Regex.Match(result, @"href=""(https://data\.gov\.lv/dati/lv/dataset/[^/]*/resource/[^/]*/download/atu_nuts_codes\.csv)""");

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
            string[] fields = ParseCsvLine(line);

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
            AtkvLevel atkvLevel = validityEnd == null ? int.Parse(level) switch
            {
                // 0 is highest
                // Latvija
                0 => AtkvLevel.Country,
                    
                // Latgale, Zemgale, Rīga, Kurzeme, Vidzeme
                1 => AtkvLevel.Region,
                
                // There is no 2 (in active)
                
                // Rīga, Daugavpils, Jelgava, ... Aizkraukles novads, Augšdaugavas novads, Balvu novads, ...
                3 => AtkvLevel.StateCityOrMunicipality,
                
                // Aizkraukle, Jaunjelgava, Koknese, ... Aiviekstes pagasts, Aizkraukles pagasts, Bebru pagasts...
                4 => AtkvLevel.CityOrParish,
                // 4 is lowest
                
                _ => throw new Exception($"Unknown ATVK level value: {level}")
            } : AtkvLevel.Expired;

            AtkvDesignation designation = validityEnd == null ? atkvLevel switch
            {
                AtkvLevel.Country                 => AtkvDesignation.Country,
                AtkvLevel.Region                  => AtkvDesignation.Region,
                AtkvLevel.StateCityOrMunicipality => name.EndsWith(" novads") ? AtkvDesignation.Municipality : AtkvDesignation.StateCity,
                AtkvLevel.CityOrParish            => name.EndsWith(" pagasts") ? AtkvDesignation.Parish : AtkvDesignation.RegionalCity,
                _                                 => throw new Exception()
            } : AtkvDesignation.Expired;

            AtkvEntry entry = new AtkvEntry(
                code,
                name,
                atkvLevel,
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
        Dictionary<string, AtkvEntry> activeEntryByCode = Entries
            .Where(e => !e.IsExpired)
            .ToDictionary(e => e.Code);

        foreach (AtkvEntry entry in Entries.Where(e => !e.IsExpired))
            if (entry.CodeParent != null && activeEntryByCode.TryGetValue(entry.CodeParent, out AtkvEntry? parent))
                entry.Parent = parent;
    }


    /// <summary>
    /// Parse a CSV line handling quoted fields properly
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        List<string> fields = [ ];
        bool inQuotes = false;
        StringBuilder currentField = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // escaped quote
                    currentField.Append('"');
                    i++; // skip next quote
                }
                else
                {
                    // toggle quote mode
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // field separator
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        // add last field
        fields.Add(currentField.ToString());

        return fields.ToArray();
    }
}

