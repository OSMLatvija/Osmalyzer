using System.Collections.Generic;

namespace Osmalyzer;

public abstract class ParcelLockerAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public abstract IEnumerable<ParcelLocker> ParcelLockers { get; }

    public override bool NeedsPreparation => true;
}