using System.Collections.Generic;

namespace Osmalyzer;

public class OmnivaParcelLockerAnalyzer : ParcelLockerAnalyzer<OmnivaParcelLockerAnalysisData>
{
    protected override string Operator => "Omniva";

    protected override List<string> ParcelLockerOsmNames => new List<string>() { Operator };
}