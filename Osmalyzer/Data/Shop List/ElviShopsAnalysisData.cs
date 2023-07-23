using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class ElviShopsAnalysisData : ShopListAnalysisData
    {
        public override string Name => "Elvi Shops";

        protected override string DataFileIdentifier => "shops-elvi";


        public override string DataFileName => cacheBasePath + DataFileIdentifier + @".html";

        public override string ShopListUrl => "https://elvi.lv/elvi-veikali/";
    }
}