using WikidataSharp;

namespace Osmalyzer;

/// <summary>
/// Wikidata entries for Latvian municipalities
/// </summary>
[UsedImplicitly]
public class MunicipalitiesWikidataData : WikidataData
{
    public override string Name => "Municipalities Wikidata";

    public override string ReportWebLink => @"https://www.wikidata.org/wiki/Q" + municipalityOfLatviaQID;

    public override bool NeedsPreparation => true;


    private const long municipalityOfLatviaQID = 3345345;


    protected override string DataFileIdentifier => "municipalities-wikidata";


    private string RawFilePath => Path.Combine(CacheBasePath, DataFileIdentifier + "-raw.json");


    public List<WikidataItem> Municipalities { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        // Fetch municipalities (e.g., Madona Municipality)
        string rawJson = Wikidata.FetchItemsByInstanceOfRaw(municipalityOfLatviaQID);
        File.WriteAllText(RawFilePath, rawJson);
        
        // Process immediately after download
        ProcessDownloadedData();
    }

    protected override void DoPrepare()
    {
        // Load from cached files and process
        ProcessDownloadedData();
    }

    
    private void ProcessDownloadedData()
    {
        string rawJson = File.ReadAllText(RawFilePath);
        Municipalities = Wikidata.ProcessItemsByInstanceOfRaw(rawJson);
        if (Municipalities.Count == 0) throw new Exception("No municipalities were fetched from Wikidata.");

        Municipalities = FilterOutDissolved(Municipalities);

#if DEBUG
        // foreach (WikidataItem item in Items)
        // {
        //     Debug.WriteLine($"Municipality: \"{item.GetLabel("lv")}\" ({item.QID}) w/ {item.Statements.Count} statements");
        //     Debug.WriteLine(" - Labels:");
        //     foreach (KeyValuePair<string, string> label in item.Labels)
        //         Debug.WriteLine($" -- {label.Key}: {label.Value}");
        //     Debug.WriteLine(" - Statements:");
        //     foreach (WikidataStatement statement in item.Statements)
        //         Debug.WriteLine($" -- P{statement.PropertyID}: {string.Join(", ", statement.Value)}");
        // }
#endif
    }



    public void Assign<T>(List<T> dataItems, Func<T, WikidataItem, bool> matcher, out List<WikidataMatchIssue> multiMatches)
        where T : class, IHasWikidataItem
    {
        AssignWikidataItems(dataItems, Municipalities, matcher, out multiMatches);
    }
}

