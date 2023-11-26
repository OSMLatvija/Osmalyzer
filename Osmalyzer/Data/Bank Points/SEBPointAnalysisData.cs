using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class SEBPointAnalysisData : BankPointAnalysisData, IPreparableAnalysisData
{
    public override string Name => "SEB Points";

    protected override string DataFileIdentifier => "seb-points";


    public List<BankPoint> Points { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://www.seb.lv/atm-find", 
            cacheBasePath + DataFileIdentifier + @".xml"
        );
        
        throw new NotImplementedException();
    }

    public void Prepare()
    {
        string data = File.ReadAllText(cacheBasePath + DataFileIdentifier + @".xml");

        Points = new List<BankPoint>();

        throw new NotImplementedException();
    }
}