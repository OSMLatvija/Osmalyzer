using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class LatviaPostLockerAnalyzer : ParcelLockerAnalyzer<ItellaParcelLockerAnalysisData>
{
    protected override string Operator => "Latvijas Pasts";
}