using System.Collections.Generic;

namespace Osmalyzer;

public class VenipakParcelLockerAnalyzer : ParcelLockerAnalyzer<VenipakParcelLockerAnalysisData>
{
    protected override string Operator => "Venipak";

    protected override List<string> ParcelLockerOsmNames => new List<string>() { Operator };
}