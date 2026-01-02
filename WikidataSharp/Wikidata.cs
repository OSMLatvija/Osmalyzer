using JetBrains.Annotations;
using Newtonsoft.Json;

namespace WikidataSharp;

public static class Wikidata
{
    private const int maxRetryWaitSeconds = 180; // 3 minutes
    private const int requestTimeoutSeconds = 20; // todo: param if higher needed


    [PublicAPI]
    [MustUseReturnValue]
    public static List<WikidataItem> FetchItemsWithProperty(long propertyID)
    {
        string rawJson = FetchItemsWithPropertyRaw(propertyID);
        return ProcessItemsWithPropertyRaw(rawJson, propertyID);
    }

    /// <summary>
    /// Fetches raw JSON response for items with a specific property
    /// </summary>
    [PublicAPI]
    [MustUseReturnValue]
    public static string FetchItemsWithPropertyRaw(long propertyID)
    {
        string filterClause = "?item wdt:P" + propertyID + " ?tempValue.";
        
        return FetchItemsWithFilterRaw(filterClause);
    }

    /// <summary>
    /// Processes raw JSON response from <see cref="FetchItemsWithPropertyRaw"/> into WikidataItem objects
    /// </summary>
    [PublicAPI]
    [MustUseReturnValue]
    public static List<WikidataItem> ProcessItemsWithPropertyRaw(string rawJson, long propertyID)
    {
        return ProcessItemsRaw(rawJson);
    }

    [PublicAPI]
    [MustUseReturnValue]
    public static List<WikidataItem> FetchItemsByInstanceOf(long instanceOfQID)
    {
        string rawJson = FetchItemsByInstanceOfRaw(instanceOfQID);
        return ProcessItemsByInstanceOfRaw(rawJson);
    }

    /// <summary>
    /// Fetches raw JSON response for items by instance of
    /// </summary>
    [PublicAPI]
    [MustUseReturnValue]
    public static string FetchItemsByInstanceOfRaw(long instanceOfQID)
    {
        string filterClause = "?item wdt:P31 wd:Q" + instanceOfQID + ".";
        return FetchItemsWithFilterRaw(filterClause);
    }

    /// <summary>
    /// Processes raw JSON response from <see cref="FetchItemsByInstanceOfRaw"/> into WikidataItem objects
    /// </summary>
    [PublicAPI]
    [MustUseReturnValue]
    public static List<WikidataItem> ProcessItemsByInstanceOfRaw(string rawJson)
    {
        return ProcessItemsRaw(rawJson);
    }


    /// <summary>
    /// Shared method to fetch items with any filter clause, avoiding Cartesian product by splitting properties and labels into separate queries
    /// </summary>
    /// <param name="filterClause">SPARQL WHERE clause to filter items (e.g., "?item wdt:P31 wd:Q515.")</param>
    [MustUseReturnValue]
    private static string FetchItemsWithFilterRaw(string filterClause)
    {
        // Fetch all properties, values, ranks, and qualifiers
        string propertiesQuery = @"SELECT DISTINCT ?item ?property ?value ?rank ?qualifierProperty ?qualifierValue WHERE { 
            " + filterClause + @"
            ?item ?p ?statement.
            ?statement ?ps ?value.
            ?property wikibase:claim ?p.
            ?property wikibase:statementProperty ?ps.
            ?statement wikibase:rank ?rank.
            OPTIONAL {
                ?statement ?pq ?qualifierValue.
                ?qualifierProperty wikibase:qualifier ?pq.
            }
        }";

        string propertiesRequestUri = $"https://query.wikidata.org/sparql?query={Uri.EscapeDataString(propertiesQuery)}&format=json";
        string propertiesJson = ExecuteRequestWithRetry(propertiesRequestUri);
        
        // Fetch labels separately to avoid Cartesian product
        string labelsQuery = @"SELECT DISTINCT ?item ?itemLabel ?itemLabelLang WHERE { 
            " + filterClause + @"
            ?item rdfs:label ?itemLabel. 
            BIND(LANG(?itemLabel) AS ?itemLabelLang)
        }";
        
        string labelsRequestUri = $"https://query.wikidata.org/sparql?query={Uri.EscapeDataString(labelsQuery)}&format=json";
        string labelsJson = ExecuteRequestWithRetry(labelsRequestUri);
        
        // Combine both results into a single JSON structure
        dynamic propertiesContent = JsonConvert.DeserializeObject(propertiesJson)!;
        dynamic labelsContent = JsonConvert.DeserializeObject(labelsJson)!;
        
        dynamic combinedResult = new
        {
            properties = propertiesContent.results.bindings,
            labels = labelsContent.results.bindings
        };
        
        return JsonConvert.SerializeObject(combinedResult, Formatting.Indented);
        
        // todo: better than double-serialize/reserialize
    }


    /// <summary>
    /// Shared method to process the combined JSON response from <see cref="FetchItemsWithFilterRaw"/> into WikidataItem objects
    /// </summary>
    [MustUseReturnValue]
    private static List<WikidataItem> ProcessItemsRaw(string rawJson)
    {
        dynamic content = JsonConvert.DeserializeObject(rawJson)!;

        // Store statements with their full context (value, datatype, rank, qualifiers, language)
        // Key structure: itemID -> propertyID -> value -> (dataType, rank, language, qualifiers)
        Dictionary<long, (Dictionary<string, string> labels, Dictionary<long, Dictionary<string, (string dataType, WikidataRank rank, string? language, Dictionary<long, string> qualifiers)>> statements)> itemsData = 
            new Dictionary<long, (Dictionary<string, string>, Dictionary<long, Dictionary<string, (string, WikidataRank, string?, Dictionary<long, string>)>>)>();

        // Process properties and values with rank and qualifiers
        foreach (dynamic binding in content.properties)
        {
            string itemUri = binding["item"]["value"];
            long wikidataID = long.Parse(itemUri[(itemUri.LastIndexOf('Q') + 1)..]);

            if (!itemsData.ContainsKey(wikidataID))
                itemsData[wikidataID] = (new Dictionary<string, string>(), new Dictionary<long, Dictionary<string, (string, WikidataRank, string?, Dictionary<long, string>)>>());

            if (binding.property != null && binding.value != null && binding.rank != null)
            {
                string propertyUri = binding.property.value;
                string valueRaw = binding.value.value;
                string valueType = binding.value.type;
                string dataType = valueType;
                
                if (binding.value.datatype != null)
                    dataType = (string)binding.value.datatype;

                long propertyID = long.Parse(propertyUri[(propertyUri.LastIndexOf('P') + 1)..]);

                // Parse rank
                string rankUri = binding.rank.value;
                WikidataRank rank = WikidataRank.Normal; // default
                if (rankUri.Contains("PreferredRank"))
                    rank = WikidataRank.Preferred;
                else if (rankUri.Contains("DeprecatedRank"))
                    rank = WikidataRank.Deprecated;

                // Extract language if present (xml:lang attribute)
                string? language = null;
                if (binding.value["xml:lang"] != null)
                    language = (string)binding.value["xml:lang"];

                if (!itemsData[wikidataID].statements.ContainsKey(propertyID))
                    itemsData[wikidataID].statements[propertyID] = new Dictionary<string, (string, WikidataRank, string?, Dictionary<long, string>)>();
                
                if (!itemsData[wikidataID].statements[propertyID].ContainsKey(valueRaw))
                    itemsData[wikidataID].statements[propertyID][valueRaw] = (dataType, rank, language, new Dictionary<long, string>());

                // Process qualifiers
                if (binding.qualifierProperty != null && binding.qualifierValue != null)
                {
                    string qualifierPropertyUri = binding.qualifierProperty.value;
                    long qualifierPropertyID = long.Parse(qualifierPropertyUri[(qualifierPropertyUri.LastIndexOf('P') + 1)..]);
                    string qualifierValueRaw = binding.qualifierValue.value;

                    if (!itemsData[wikidataID].statements[propertyID][valueRaw].qualifiers.ContainsKey(qualifierPropertyID))
                        itemsData[wikidataID].statements[propertyID][valueRaw].qualifiers[qualifierPropertyID] = qualifierValueRaw;
                }
            }
        }

        // Process labels separately
        foreach (dynamic binding in content.labels)
        {
            string itemUri = binding["item"]["value"];
            long wikidataID = long.Parse(itemUri[(itemUri.LastIndexOf('Q') + 1)..]);

            if (!itemsData.ContainsKey(wikidataID))
                itemsData[wikidataID] = (new Dictionary<string, string>(), new Dictionary<long, Dictionary<string, (string, WikidataRank, string?, Dictionary<long, string>)>>());

            if (binding.itemLabel != null && binding.itemLabelLang != null)
            {
                string label = (string)binding.itemLabel.value;
                string lang = (string)binding.itemLabelLang.value;
                
                if (!itemsData[wikidataID].labels.ContainsKey(lang))
                    itemsData[wikidataID].labels[lang] = label;
            }
        }

        List<WikidataItem> items = [ ];

        foreach (KeyValuePair<long, (Dictionary<string, string> labels, Dictionary<long, Dictionary<string, (string dataType, WikidataRank rank, string? language, Dictionary<long, string> qualifiers)>> statements)> kvp in itemsData)
        {
            List<WikidataStatement> statementsList = [ ];
            
            foreach (KeyValuePair<long, Dictionary<string, (string dataType, WikidataRank rank, string? language, Dictionary<long, string> qualifiers)>> stmtKvp in kvp.Value.statements)
            {
                foreach (KeyValuePair<string, (string dataType, WikidataRank rank, string? language, Dictionary<long, string> qualifiers)> valueInfo in stmtKvp.Value)
                    statementsList.Add(new WikidataStatement(stmtKvp.Key, valueInfo.Key, valueInfo.Value.dataType, valueInfo.Value.rank, valueInfo.Value.language, valueInfo.Value.qualifiers));
            }
            
            items.Add(
                new WikidataItem(
                    kvp.Key,
                    kvp.Value.labels,
                    statementsList
                )
            );
        }

        return items;
    }


    
    /// <summary>
    /// Performs an HTTP GET request with 429 (Too Many Requests) retry handling
    /// </summary>
    private static string ExecuteRequestWithRetry(string requestUri)
    {
        HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        
        using HttpClient httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Osmalyzer");
        httpClient.Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds);

        while (true)
        {
            HttpResponseMessage response = httpClient.GetAsync(requestUri).Result;
            
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                int retryAfterSeconds = 60; // default to 60 seconds if header not present
                
                if (response.Headers.RetryAfter != null)
                {
                    if (response.Headers.RetryAfter.Delta.HasValue)
                        retryAfterSeconds = (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;
                    else if (response.Headers.RetryAfter.Date.HasValue)
                        retryAfterSeconds = (int)(response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow).TotalSeconds;
                }
                
                if (retryAfterSeconds > maxRetryWaitSeconds)
                    throw new InvalidOperationException($"Rate limit exceeded. Retry-After header indicates waiting {retryAfterSeconds} seconds, which exceeds maximum wait time of {maxRetryWaitSeconds} seconds.");
                
                Thread.Sleep(TimeSpan.FromSeconds(retryAfterSeconds));
                continue;
            }
            
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().Result;
        }
    }
}