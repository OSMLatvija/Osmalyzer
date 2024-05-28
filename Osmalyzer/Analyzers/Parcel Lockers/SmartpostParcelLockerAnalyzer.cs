using System.Collections.Generic;

namespace Osmalyzer;

public class SmartpostParcelLockerAnalyzer : ParcelLockerAnalyzer<SmartpostParcelLockerAnalysisData>
{
    protected override string Operator => "Smartpost";

    protected override List<string> ParcelLockerOsmNames => new List<string>() { Operator, "Itella" };
}