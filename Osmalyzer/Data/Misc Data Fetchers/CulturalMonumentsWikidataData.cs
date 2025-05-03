using WikidataSharp;

namespace Osmalyzer;

[UsedImplicitly]
public class CulturalMonumentsWikidataData : AnalysisData
    // todo: to generic reusable wikidata provider data
{
    public override string Name => "Cultural Monuments Wikidata";

    public override string ReportWebLink => @"https://www.wikidata.org/wiki/Property:P" + PropertyID;

    public override bool NeedsPreparation => false;


    public long PropertyID => 2494; // "Latvian cultural heritage register ID"
    

    protected override string DataFileIdentifier => "cultural-monuments-wikidata";


    public List<WikidataItem> Items { get; private set; } = null!; // only null before prepared
    

    protected override void Download()
    {
        Items = Wikidata.FetchItemsWithProperty(PropertyID);
        // todo: cache, would need fetch and parse
    }

    protected override void DoPrepare()
    {
        throw new InvalidOperationException();
    }


    public void Assign(List<CulturalMonument> monuments) // todo: interface
    {
        foreach (CulturalMonument monument in monuments)
        {
            if (monument.ReferenceID != null)
            {
                string refIdStr = monument.ReferenceID.Value.ToString();
                WikidataItem? wikidataItem = Items.FirstOrDefault(i => i[PropertyID] == refIdStr);

                if (wikidataItem != null)
                    monument.WikidataItem = wikidataItem;
            }
        }
    }
}