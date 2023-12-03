using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using WikidataSharp;

namespace Osmalyzer;

[UsedImplicitly]
public class CulturalMonumentsWikidataData : AnalysisData
    // todo: to generic reusable wikidata provider data
{
    public override string Name => "Cultural Monuments Wikidata";

    public override string ReportWebLink => @"https://www.wikidata.org/wiki/Property:P2494";


    protected override string DataFileIdentifier => "cultural-monuments-wikidata";


    public List<WikidataItem> Items { get; private set; } = null!; // only null before prepared
    

    protected override void Download()
    {
        Items = Wikidata.FetchItemsWithProperty(2494);
        // todo: cache, would need fetch and parse
    }

    
    public void Assign(List<CulturalMonument> monuments) // todo: interface
    {
        foreach (CulturalMonument monument in monuments)
        {
            if (monument.ReferenceID != null)
            {
                string refIdStr = monument.ReferenceID.Value.ToString();
                WikidataItem? wikidataItem = Items.FirstOrDefault(i => i[2494] == refIdStr);

                if (wikidataItem != null)
                    monument.WikidataItem = wikidataItem;
            }
        }
    }
}