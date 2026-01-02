using System.Diagnostics;
using WikidataSharp;

namespace Osmalyzer;

/// <summary>
/// Wikidata entries for Latvian villages and hamlets
/// </summary>
[UsedImplicitly]
public class VillagesWikidataData : AdminWikidataData
{
    public override string Name => "Villages Wikidata";

    public override string ReportWebLink => @"https://www.wikidata.org/wiki/Q" + villageInLatviaQID;

    public override bool NeedsPreparation => false;


    private const long villageInLatviaQID = 22580836; // includes both villages and hamlets


    protected override string DataFileIdentifier => "villages-wikidata";


    public List<WikidataItem> Items { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        // Fetch villages and hamlets (e.g., village Ulbroka, hamlet Pilda)
        // Note: Wikidata doesn't differentiate between villages and hamlets - both use the same instance-of
        Items = Wikidata.FetchItemsByInstanceOf(villageInLatviaQID);
        if (Items.Count == 0) throw new Exception("No villages were fetched from Wikidata.");

#if DEBUG
        foreach (WikidataItem item in Items) Debug.WriteLine($"Village/Hamlet: \"{item.GetLabel("lv")}\" ({item.QID}) w/ {item.Statements.Count} statements");
#endif
    }

    protected override void DoPrepare()
    {
        throw new InvalidOperationException();
    }


    public void Assign<T>(List<T> dataItems, Func<T, string> dataItemNameLookup, Action<T, WikidataItem> dataItemAssigner)
    {
        AssignWikidataItems(dataItems, Items, dataItemNameLookup, dataItemAssigner);
    }
}


