﻿using WikidataSharp;

namespace Osmalyzer;

[UsedImplicitly]
public class CulturalMonumentsWikidataData : AnalysisData, IUndatedAnalysisData
    // todo: to generic reusable wikidata provider data
{
    public override string Name => "Cultural Monuments Wikidata";

    public override string ReportWebLink => @"https://www.wikidata.org/wiki/Property:P" + PropertyID;

    public override bool NeedsPreparation => true;


    public long PropertyID => 2494; // "Latvian cultural heritage register ID"
    

    protected override string DataFileIdentifier => "cultural-monuments-wikidata";


    private string RawFilePath => Path.Combine(CacheBasePath, DataFileIdentifier + "-raw.json");


    public List<WikidataItem> Items { get; private set; } = null!; // only null before prepared
    

    protected override void Download()
    {
        string rawJson = Wikidata.FetchItemsWithPropertyRaw(PropertyID);
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
        Items = Wikidata.ProcessItemsWithPropertyRaw(rawJson, PropertyID);
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