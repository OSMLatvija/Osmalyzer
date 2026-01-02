using System.Diagnostics;
using WikidataSharp;

namespace Osmalyzer;

/// <summary>
/// Wikidata entries for Latvian parishes
/// </summary>
[UsedImplicitly]
public class ParishesWikidataData : AdminWikidataData
{
    public override string Name => "Parishes Wikidata";

    public override string ReportWebLink => @"https://www.wikidata.org/wiki/Q" + parishOfLatviaQID;

    public override bool NeedsPreparation => false;


    private const long parishOfLatviaQID = 2577184;


    protected override string DataFileIdentifier => "parishes-wikidata";


    public List<WikidataItem> Items { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        // Fetch parishes (e.g., Aglona Parish)
        Items = Wikidata.FetchItemsByInstanceOf(parishOfLatviaQID);
        if (Items.Count == 0) throw new Exception("No parishes were fetched from Wikidata.");

#if DEBUG
        foreach (WikidataItem item in Items) Debug.WriteLine($"Parish: \"{item.GetLabel("lv")}\" ({item.QID}) w/ {item.Statements.Count} statements");
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


