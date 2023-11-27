﻿using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class CulturalMonumentsAPIAnalysisData : AnalysisData, IPreparableAnalysisData, IUndatedAnalysisData
{
    public override string Name => "Cultural Monuments";

    public override string ReportWebLink => @"https://karte.mantojums.lv";


    protected override string DataFileIdentifier => "cultural-monuments";


    public List<CulturalMonument> Monuments { get; private set; } = null!; // only null before prepared
        

    protected override void Download()
    {
        if (!Directory.Exists(cacheBasePath + DataFileIdentifier))
            Directory.CreateDirectory(cacheBasePath + DataFileIdentifier);
        
        // https://api.mantojums.lv/docs/index.html
        // https://api.mantojums.lv/api/CulturalObjects?group=Monument&Page=1
        // https://api.mantojums.lv/api/CulturalObjects?group=Monument&Page=1&ShouldPage=false&ShouldLimit=false -- doesn't do anything

        // TODO: ALL PAGES
        
        for (int i = 1; i <= 3; i++)
        {
            WebsiteDownloadHelper.Download(
                "https://api.mantojums.lv/api/CulturalObjects?group=Monument&Page=" + i + @"&ShouldPage=false&ShouldLimit=false",
                cacheBasePath + DataFileIdentifier + "/" + i  + @".json"
            );
        }
    }

    public void Prepare()
    {
        Monuments = new List<CulturalMonument>();
        
        string[] files = Directory.GetFiles(cacheBasePath + DataFileIdentifier + "/", "*.json");

        for (int i = 0; i < files.Length; i++)
        {
            string contentString = File.ReadAllText(files[i]);
            
            dynamic content = JsonConvert.DeserializeObject(contentString)!;

            int total = content.total; // e.g. 7000
            int count = content.pageSize; // e.g. 30

            for (int k = 0; k < count; k++)
            {
                dynamic item = content.items[0];

                string name = item.name;

                int id = item.protectionNumber;

                OsmCoord osmCoord = new OsmCoord(56, 24);
                // TODO: it's not in the data here, but each individual monument request
                
                Monuments.Add(new CulturalMonument(osmCoord, name, id));
            }
        }
    }
}