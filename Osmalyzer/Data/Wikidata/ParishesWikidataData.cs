using WikidataSharp;

namespace Osmalyzer;

/// <summary>
/// Wikidata entries for Latvian parishes
/// </summary>
[UsedImplicitly]
public class ParishesWikidataData : WikidataData
{
    public override string Name => "Parishes Wikidata";

    public override string ReportWebLink => @"https://www.wikidata.org/wiki/Q" + parishOfLatviaQID;

    public override bool NeedsPreparation => true;


    private const long parishOfLatviaQID = 2577184;


    protected override string DataFileIdentifier => "parishes-wikidata";


    private string RawFilePath => Path.Combine(CacheBasePath, DataFileIdentifier + "-raw.json");


    public List<WikidataItem> Parishes { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        // Fetch parishes (e.g., Aglona Parish)
        string rawJson = Wikidata.FetchItemsByInstanceOfRaw(parishOfLatviaQID);
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
        Parishes = Wikidata.ProcessItemsByInstanceOfRaw(rawJson);
        if (Parishes.Count == 0) throw new Exception("No parishes were fetched from Wikidata.");

        Parishes = FilterOutDissolved(Parishes);

#if DEBUG
        // foreach (WikidataItem item in Items) Debug.WriteLine($"Parish: \"{item.GetLabel("lv")}\" ({item.QID}) w/ {item.Statements.Count} statements");
#endif
    }



    public void Assign<T>(List<T> dataItems, Func<T, WikidataItem, bool> matcher, out List<WikidataMatchIssue> multiMatches)
        where T : class, IHasWikidataItem
    {
        AssignWikidataItems(dataItems, Parishes, matcher, out multiMatches);
    }
}


