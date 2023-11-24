using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class LatsShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "LaTS Shops";

        
    protected override string DataFileIdentifier => "shops-lats";


    public override string DataFileName => cacheBasePath + DataFileIdentifier + @".html";

    public override string ShopListUrl => "https://www.latts.lv/lats-veikali";
}