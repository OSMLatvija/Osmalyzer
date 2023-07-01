using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class TopShopsAnalysisData : ShopListAnalysisData
    {
        public override string Name => "Top! Shops";

        protected override string DataFileIdentifier => "shops-top";


        public override string DataFileName => cacheBasePath + DataFileIdentifier + @".html";

        public override string ShopListUrl => "https://www.toppartika.lv/veikali/";
    }
}