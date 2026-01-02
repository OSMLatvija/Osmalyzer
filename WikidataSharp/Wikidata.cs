using JetBrains.Annotations;
using Newtonsoft.Json;

namespace WikidataSharp;

public static class Wikidata
{
    [PublicAPI]
    [MustUseReturnValue]
    public static List<WikidataItem> FetchItemsWithProperty(long propertyID)
    {
        string query = @"SELECT DISTINCT ?item ?value ?itemLabel ?itemLabelLang WHERE { 
            ?item wdt:P" + propertyID + @" ?value. 
            OPTIONAL { ?item rdfs:label ?itemLabel. BIND(LANG(?itemLabel) AS ?itemLabelLang) }
        }";

        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Osmalyzer");

        string requestUri = $"https://query.wikidata.org/sparql?query={Uri.EscapeDataString(query)}&format=json";
        
        string requestResult = httpClient.GetStringAsync(requestUri).Result;

        dynamic content = JsonConvert.DeserializeObject(requestResult)!;
        
        Dictionary<long, (string value, string dataType, Dictionary<string, string> labels)> itemsData = new Dictionary<long, (string, string, Dictionary<string, string>)>();

        foreach (dynamic binding in content.results.bindings)
        {
            string itemUri = binding["item"]["value"];
            string valueRaw = binding["value"]["value"];
            string valueType = binding["value"]["type"];
            string dataType = valueType;
            
            if (binding["value"]["datatype"] != null)
                dataType = (string)binding["value"]["datatype"];

            long wikidataID = long.Parse(itemUri[(itemUri.LastIndexOf('Q') + 1)..]);

            if (!itemsData.ContainsKey(wikidataID))
                itemsData[wikidataID] = (valueRaw, dataType, new Dictionary<string, string>());

            if (binding.itemLabel != null && binding.itemLabelLang != null)
            {
                string label = (string)binding.itemLabel.value;
                string lang = (string)binding.itemLabelLang.value;
                
                itemsData[wikidataID].labels[lang] = label;
            }
        }

        List<WikidataItem> items = [ ];

        foreach (KeyValuePair<long, (string value, string dataType, Dictionary<string, string> labels)> kvp in itemsData)
        {
            items.Add(
                new WikidataItem(
                    kvp.Key,
                    kvp.Value.labels, [ new WikidataStatement(propertyID, kvp.Value.value, kvp.Value.dataType) ]
                )
            );
        }

        return items;
    }

    [PublicAPI]
    [MustUseReturnValue]
    public static List<WikidataItem> FetchItemsByInstanceOf(long instanceOfQID)
    {
        // Query for items where P31 (instance of) equals the specified QID, and fetch all their statements
        string query = @"SELECT ?item ?property ?value ?itemLabel ?itemLabelLang WHERE { 
            ?item wdt:P31 wd:Q" + instanceOfQID + @". 
            ?item ?p ?value.
            ?property wikibase:directClaim ?p.
            OPTIONAL { ?item rdfs:label ?itemLabel. BIND(LANG(?itemLabel) AS ?itemLabelLang) }
        }";

        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Osmalyzer");

        string requestUri = $"https://query.wikidata.org/sparql?query={Uri.EscapeDataString(query)}&format=json";
        
        string requestResult = httpClient.GetStringAsync(requestUri).Result;

        dynamic content = JsonConvert.DeserializeObject(requestResult)!;

        Dictionary<long, (Dictionary<string, string> labels, Dictionary<long, Dictionary<string, string>> statements)> itemsData = new Dictionary<long, (Dictionary<string, string>, Dictionary<long, Dictionary<string, string>>)>();

        foreach (dynamic binding in content.results.bindings)
        {
            string itemUri = binding["item"]["value"];

            long wikidataID = long.Parse(itemUri[(itemUri.LastIndexOf('Q') + 1)..]);

            if (!itemsData.ContainsKey(wikidataID))
                itemsData[wikidataID] = (new Dictionary<string, string>(), new Dictionary<long, Dictionary<string, string>>());

            if (binding.itemLabel != null && binding.itemLabelLang != null)
            {
                string label = (string)binding.itemLabel.value;
                string lang = (string)binding.itemLabelLang.value;
                
                if (!itemsData[wikidataID].labels.ContainsKey(lang))
                    itemsData[wikidataID].labels[lang] = label;
            }

            if (binding.property != null && binding.value != null)
            {
                string propertyUri = binding.property.value;
                string valueRaw = binding.value.value;
                string valueType = binding.value.type;
                string dataType = valueType;
                
                if (binding.value.datatype != null)
                    dataType = (string)binding.value.datatype;

                long propertyID = long.Parse(propertyUri[(propertyUri.LastIndexOf('P') + 1)..]);

                if (!itemsData[wikidataID].statements.ContainsKey(propertyID))
                    itemsData[wikidataID].statements[propertyID] = new Dictionary<string, string>();
                
                // Store value with its datatype (key = value, value = datatype)
                if (!itemsData[wikidataID].statements[propertyID].ContainsKey(valueRaw))
                    itemsData[wikidataID].statements[propertyID][valueRaw] = dataType;
            }
        }

        List<WikidataItem> items = [ ];

        foreach (KeyValuePair<long, (Dictionary<string, string> labels, Dictionary<long, Dictionary<string, string>> statements)> kvp in itemsData)
        {
            List<WikidataStatement> statementsList = [ ];
            
            foreach (KeyValuePair<long, Dictionary<string, string>> stmtKvp in kvp.Value.statements)
            {
                foreach (KeyValuePair<string, string> valueDataType in stmtKvp.Value)
                    statementsList.Add(new WikidataStatement(stmtKvp.Key, valueDataType.Key, valueDataType.Value));
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
}