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
            string areaCode = areaCodes[i];
            
            string areaName = areaDimension.Category.Label[areaCode];
            areaName = areaName.TrimStart('.'); // Many entries are like "..Rūjiena" or "..Silmalas pagasts"
            
            // todo: "Zemgales statistiskais reģions"
            // todo: "Rīgas statistiskais reģions (no 01.01.2024.)"
            // todo: "Salas pagasts (Jēkabpils novads)" / "Pilskalnes pagasts (Aizkraukles novads)"
            
            // todo: type
            
            int? population = content.Value[i];

            // Skip historic regions without current data
            if (population == null)
                continue;

            Entries.Add(new CspPopulationEntry(areaCode, areaName, population.Value));
        }
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
    public string AreaCode { get; }
    
    public string AreaName { get; }
    
    public int Population { get; }

    public OsmCoord Coord => throw new NotImplementedException();
    
    public string Name => AreaName;
    
   
    public CspPopulationEntry(string areaCode, string areaName, int population)
    {
        AreaCode = areaCode;
        AreaName = areaName;
        Population = population;
    }
    
    
    public string ReportString()
    {
        return
            "`" + AreaName + "` #`" + AreaCode + "` `" + Population + "`";
    }
}

