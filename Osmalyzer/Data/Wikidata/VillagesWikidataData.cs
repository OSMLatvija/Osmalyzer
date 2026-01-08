using WikidataSharp;

namespace Osmalyzer;

/// <summary>
/// Wikidata entries for Latvian villages and hamlets
/// </summary>
[UsedImplicitly]
public class VillagesWikidataData : WikidataData
{
    public override string Name => "Villages Wikidata";

    public override string ReportWebLink => @"https://www.wikidata.org/wiki/Q" + villageInLatviaQID;

    public override bool NeedsPreparation => true;


    private const long villageInLatviaQID = 22580836; // ciems proper (currently includes all villages and hamlets from old scheme)
    private const long hamletInLatviaQID = 137705728; // mazciems proper


    protected override string DataFileIdentifier => "villages-wikidata";


    private string RawFilePath => Path.Combine(CacheBasePath, DataFileIdentifier + "-raw.json");


    public List<WikidataItem> AllVillages { get; private set; } = null!; // only null before prepared

    // todo: once wikidata itself is fixed
    // public List<WikidataItem> Hamlets { get; private set; } = null!; // only null before prepared
    // public List<WikidataItem> NonHamlets { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        // Fetch villages and hamlets (e.g., village Ulbroka, hamlet Pilda)
        // Note: Wikidata doesn't directly differentiate between villages and hamlets - both use the same instance-of
        string rawJson = Wikidata.FetchItemsByInstanceOfRaw(villageInLatviaQID, hamletInLatviaQID);
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
        AllVillages = Wikidata.ProcessItemsByInstanceOfRaw(rawJson);
        if (AllVillages.Count == 0) throw new Exception("No villages were fetched from Wikidata.");

        AllVillages = FilterOutDissolved(AllVillages);
        
        // Hamlets = AllVillages
        //     .Where(item => item.HasActiveStatementValueAsQID(WikiDataProperty.InstanceOf, smallVillageInLatviaQID))
        //     .ToList();
        // if (Hamlets.Count == 0) throw new Exception("No hamlets were identified among the villages from Wikidata, which is not expected and probably means Wikidata has changed something");
        //
        // NonHamlets = AllVillages.Except(Hamlets).ToList();
        // if (NonHamlets.Count == 0) throw new Exception("All villages were classified as hamlets, which is not expected and probably means Wikidata has changed something");

#if DEBUG
        // foreach (WikidataItem item in Items) Debug.WriteLine($"Village/Hamlet: \"{item.GetLabel("lv")}\" ({item.QID}) w/ {item.Statements.Count} statements");
#endif
    }


    public void AssignVillageOrHamlet<T>(List<T> dataItems, Func<T, WikidataItem, bool> matcher, double coordMismatchDistance, out List<WikidataMatchIssue> issues) 
        where T : class, IDataItem, IHasWikidataItem
    {
        AssignWikidataItems(dataItems, AllVillages, matcher, coordMismatchDistance, out issues);
    }

    // public void AssignHamlets<T>(List<T> dataItems, Func<T, WikidataItem, bool> matcher, out List<WikidataMatchIssue> issues) 
    //     where T : class, IHasWikidataItem
    // {
    //     AssignWikidataItems(dataItems, Hamlets, matcher, out issues);
    // }
    //
    // public void AssignNonHamlets<T>(List<T> dataItems, Func<T, WikidataItem, bool> matcher, out List<WikidataMatchIssue> issues)
    //     where T : class, IHasWikidataItem
    // {
    //     AssignWikidataItems(dataItems, NonHamlets, matcher, out issues);
    // }
}


