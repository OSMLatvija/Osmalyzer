using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class MaximaShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Maxima Shops";

    protected override string DataFileIdentifier => "shops-maxima";


    public override string DataFileName => cacheBasePath + DataFileIdentifier + @".html";

    public override string ShopListUrl => "https://www.maxima.lv/veikalu-kedes";


    public override List<ShopData> GetShops()
    {
        //
                
        string source = File.ReadAllText(DataFileName);
        
        List<ShopData> listedShops = new List<ShopData>();
                
        throw new NotImplementedException();
    }
}