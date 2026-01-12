using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class CspPopulationAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "CSP Population Statistics";

    public override string ReportWebLink => @"https://data.stat.gov.lv/pxweb/lv/OSP_PUB/START__POP__IR__IRS/IRS051/";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "csp";


    public List<CspPopulationEntry> Entries { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        // PxWeb API documentation: https://www.scb.se/en/services/open-data-api/api-for-the-statistical-database/
        // CSP uses PxWeb for their statistical database
        
        string metadataUrl = "https://data.stat.gov.lv/api/v1/lv/OSP_PUB/START/POP/IR/IRS/IRS051";
        string metadataJson = WebsiteDownloadHelper.Read(metadataUrl, true);
        PxWebMetadata metadata = JsonConvert.DeserializeObject<PxWebMetadata>(metadataJson)!;
        
        // Extract all AREA codes from metadata
        List<string> areaCodes = [ ];
        
        foreach (PxWebVariable variable in metadata.Variables)
        {
            if (variable.Code == "AREA")
            {
                areaCodes.AddRange(variable.Values);
                break;
            }
        }
        
        // Step 2: Build area values list for JSON request
        string areaValuesJson = string.Join(",", areaCodes.Select(code => $"\"{code}\""));
        
        // Step 3: Request data for all areas
        string requestBody = @"{
  ""query"": [
    {
      ""code"": ""INDICATOR"",
      ""selection"": {
        ""filter"": ""item"",
        ""values"": [""TOTAL""]
      }
    },
    {
      ""code"": ""AREA"",
      ""selection"": {
        ""filter"": ""item"",
        ""values"": [" + areaValuesJson + @"]
      }
    },
    {
      ""code"": ""TIME"",
      ""selection"": {
        ""filter"": ""item"",
        ""values"": [""2025""]
      }
    }
  ],
  ""response"": {
    ""format"": ""json-stat2""
  }
}";

        WebsiteDownloadHelper.DownloadPostJson(
            metadataUrl,
            requestBody,
            Path.Combine(CacheBasePath, DataFileIdentifier + @".json")
        );
    }

    protected override void DoPrepare()
    {
        Entries = [ ];
        
        string dataFileName = Path.Combine(CacheBasePath, DataFileIdentifier + @".json");
        
        string contentString = File.ReadAllText(dataFileName);
        
        JsonStatResponse content = JsonConvert.DeserializeObject<JsonStatResponse>(contentString)!;

        // JSON-stat2 format structure:
        // - dimension.AREA contains area codes and labels
        // - value[] contains population values (can be null for historic regions)
        // - The values array is indexed by AREA (since INDICATOR and TIME are eliminated/fixed)

        JsonStatDimension areaDimension = content.Dimension["AREA"];
        
        // Get area codes in order by building array from index dictionary
        string[] areaCodes = new string[areaDimension.Category.Index.Count];
        
        foreach (KeyValuePair<string, int> indexEntry in areaDimension.Category.Index)
        {
            string code = indexEntry.Key;
            int index = indexEntry.Value;
            areaCodes[index] = code;
        }

        // Process each area
        for (int i = 0; i < areaCodes.Length; i++)
        {
            int? population = content.Value[i];
            if (population == null)
                continue; // skip historic regions without data
            
            string areaCode = areaCodes[i];

            string areaName = areaDimension.Category.Label[areaCode].Trim();
            areaName = areaName.TrimStart('.'); // Many entries are like "..Rūjiena" or "..Silmalas pagasts"
            
            // "Latvija"
            // "Valkas novads"
            // "Jersikas pagasts"
            // "Salas pagasts (Jēkabpils novads)"
            // "Ludza"
            // "Zemgales statistiskais reģions"
            // "Rīgas statistiskais reģions (no 01.01.2024.)"
            // "Nezināma teritoriālā vienība"

            string? id;
            CspAreaType type;
            string? municipality = null;

            if (areaName == "Nezināma teritoriālā vienība") // known name for unknown area
            {
                if (areaCode != "UNK") throw new Exception("Unexpected area code for unknown area: " + areaCode);
               
                type = CspAreaType.Unlocated;
                id = null;
            }
            else
            {
                if (areaName == "Latvija") // known name for country, only one entry, but ambiguous to cities if we don't special-case
                {
                    if (areaCode != "LV") throw new Exception("Unexpected area code for country: " + areaCode);
                   
                    type = CspAreaType.Country;
                    id = null;
                }
                else
                {
                    if (!areaCode.StartsWith("LV")) throw new Exception("Unexpected area code format: " + areaCode);
                    id = areaCode[2..]; // Remove "LV" prefix
                    if (id == "") throw new Exception("Unexpected empty area code after removing LV prefix");
                    
                    // Qualifier?
                    Match qualifierRegex = Regex.Match(areaName, @"\((.*?)\)$");
                    string? qualifier = qualifierRegex.Success ? qualifierRegex.Groups[1].Value : null;
                    if (qualifier != null)
                        areaName = areaName[..(areaName.Length - qualifier.Length - 2)].TrimEnd();

                    if (areaName.EndsWith(" novads"))
                    {
                        type = CspAreaType.Municipality;
                        if (qualifier != null) throw new Exception("Unexpected qualifier for municipality: " + areaName);
                    }
                    else if (areaName.EndsWith(" pagasts"))
                    {
                        type = CspAreaType.Parish;
                        municipality = qualifier; // possible disambiguation
                    }
                    else if (areaName.EndsWith(" statistiskais reģions"))
                    {
                        type = CspAreaType.Region;
                    }
                    else
                    {
                        type = CspAreaType.City; // assume city if nothing else matches
                        if (qualifier != null) throw new Exception("Unexpected qualifier for city: " + areaName);
                    }
                }
            }

            Entries.Add(new CspPopulationEntry(id, areaName, municipality, population.Value, type));
        }
    }

    
    public void AssignToDataItems<T>(
        List<T> items, 
        CspAreaType type,
        Func<T, string> nameLookup,
        Func<T, string?> disambiguatorLookup)
        where T : class, IDataItem, IHasCspPopulationEntry
    {
        int assigned = 0;
        
        foreach (T item in items)
        {
            string itemName = nameLookup(item);
            string? itemDisambiguator = disambiguatorLookup(item);

            List<CspPopulationEntry> matchedEntries = Entries.Where(entry => entry.Type == type &&
                                                                             entry.Name == itemName &&
                                                                             (entry.Municipality == itemDisambiguator || entry.Municipality == null)
            ).ToList();

            if (matchedEntries.Count == 0)
                continue;
            
            if (matchedEntries.Count > 1) throw new Exception("Multiple CSP population entries found for item: " + item.ReportString());
            
            item.CspPopulationEntry = matchedEntries[0];
            assigned++;
        }
        
        if (assigned == 0)
            throw new Exception("No CSP population entries were assigned for type " + type);
    }


    /// <summary>
    /// PxWeb API metadata response structure
    /// </summary>
    private class PxWebMetadata
    {
        [JsonProperty("variables")]
        public List<PxWebVariable> Variables { get; set; } = null!;
    }


    /// <summary>
    /// PxWeb API variable metadata
    /// </summary>
    private class PxWebVariable
    {
        [JsonProperty("code")]
        public string Code { get; set; } = null!;

        [JsonProperty("values")]
        public List<string> Values { get; set; } = null!;
    }


    /// <summary>
    /// JSON-stat2 format response structure
    /// </summary>
    private class JsonStatResponse
    {
        [JsonProperty("dimension")]
        public Dictionary<string, JsonStatDimension> Dimension { get; set; } = null!;

        [JsonProperty("value")]
        public List<int?> Value { get; set; } = null!; // nullable for historic regions
    }


    /// <summary>
    /// JSON-stat2 dimension structure
    /// </summary>
    private class JsonStatDimension
    {
        [JsonProperty("category")]
        public JsonStatCategory Category { get; set; } = null!;
    }


    /// <summary>
    /// JSON-stat2 category structure containing index and label mappings
    /// </summary>
    private class JsonStatCategory
    {
        [JsonProperty("index")]
        public Dictionary<string, int> Index { get; set; } = null!;

        [JsonProperty("label")]
        public Dictionary<string, string> Label { get; set; } = null!;
    }
}


public class CspPopulationEntry : IDataItem
{
    public string? Id { get; }
    
    public CspAreaType Type { get; }
    
    public string Name { get; }
    
    public string? Municipality { get; }

    public int Population { get; }

    public OsmCoord Coord => throw new NotImplementedException();
    
   
    public CspPopulationEntry(string? id, string name, string? municipality, int population, CspAreaType type)
    {
        Id = id;
        Name = name;
        Municipality = municipality;
        Population = population;
        Type = type;
    }


    public string ReportString()
    {
        return
            Type +
            " `" + Name + "`" +
            (Municipality != null ? " (in `" + Municipality + "`)" : "") +
            (Id != null ? " #`" + Id + "`" : "") +
             " `" + Population + "`";
    }
}

public enum CspAreaType
{
    Country,
    Region,
    Municipality,
    Parish,
    City,
    Unlocated
}


public interface IHasCspPopulationEntry
{
    CspPopulationEntry? CspPopulationEntry { get; set; }
}