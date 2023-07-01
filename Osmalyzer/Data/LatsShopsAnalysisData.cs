using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class LatsShopsAnalysisData : ShopListAnalysisData
    {
        public override string Name => "LaTS Shops";


        public override string DataFileName => @"cache/lats-shops.html";

        public override string ShopListUrl => "https://www.latts.lv/lats-veikali";
    }
}