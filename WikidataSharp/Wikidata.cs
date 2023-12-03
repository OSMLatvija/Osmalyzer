using JetBrains.Annotations;
using Newtonsoft.Json;

namespace WikidataSharp;

public static class Wikidata
{
    [PublicAPI]
    [MustUseReturnValue]
    public static List<WikidataItem> FetchItemsWithProperty(long propertyID)
    {
        string query = @"SELECT DISTINCT ?item ?value WHERE { ?item wdt:P" + propertyID + @" ?value. }";

        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Osmalyzer");

        string requestUri = $"https://query.wikidata.org/sparql?query={Uri.EscapeDataString(query)}&format=json";
        
        string requestResult = httpClient.GetStringAsync(requestUri).Result;

        dynamic content = JsonConvert.DeserializeObject(requestResult)!;

        // List<string> vars = new List<string>();
        // foreach (string var in content.head.vars)
        //     vars.Add(var);
        // // These will be "item" and "value" as requested

        // Expecting item to be:
        // "item" : {
        //     "type" : "uri",
        //     "value" : "http://www.wikidata.org/entity/Q12649814"
        // },
        // and value:
        // "value" : {
        //     "type" : "literal",
        //     "value" : "112"
        // }
        
        List<WikidataItem> items = new List<WikidataItem>();

        foreach (dynamic binding in content.results.bindings)
        {
            string itemUri = binding["item"]["value"];
            string valueRaw = binding["value"]["value"];

            long wikidataID = long.Parse(itemUri[(itemUri.LastIndexOf('Q') + 1)..]);
            
            items.Add(
                new WikidataItem(
                    wikidataID, 
                    new List<WikidataStatement>() { new WikidataStatement(propertyID, valueRaw) }
                )
            );
        }

        return items;
    }
}