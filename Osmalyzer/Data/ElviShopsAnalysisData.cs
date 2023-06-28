using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class ElviShopsAnalysisData : ShopListAnalysisData
    {
        public override string Name => "Elvi Shops";
        
        public override string DataFileName => @"cache/elvi-shops.html";

        public override string ShopListUrl => "https://elvi.lv/elvi-veikali/";
    }
}