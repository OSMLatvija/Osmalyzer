using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class TopShopsAnalysisData : ShopListAnalysisData
    {
        public override string Name => "Top! Shops";


        public override string DataFileName => @"cache/top-shops.html";

        public override string ShopListUrl => "https://www.toppartika.lv/veikali/";
    }
}