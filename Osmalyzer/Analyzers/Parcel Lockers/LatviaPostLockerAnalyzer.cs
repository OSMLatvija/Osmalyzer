using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class LatviaPostLockerAnalyzer : ParcelLockerAnalyzer<LatviaPostAnalysisData>
{
    protected override string Operator => "Latvijas Pasts";
}