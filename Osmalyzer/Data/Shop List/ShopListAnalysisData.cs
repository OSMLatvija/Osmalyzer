using System.Collections.Generic;

namespace Osmalyzer;

public abstract class ShopListAnalysisData : AnalysisData, IPreparableAnalysisData, IUndatedAnalysisData
{
    public abstract string DataFileName { get; }


    public abstract IEnumerable<ShopData> Shops { get; }


    public abstract void Prepare();
}