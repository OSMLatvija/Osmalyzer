using System.Collections.Generic;

namespace Osmalyzer;

public class UnisendParcelLockerAnalyzer : ParcelLockerAnalyzer<UnisendParcelLockerAnalysisData>
{
    protected override string Operator => "Unisend";

    protected override List<ValidationRule>? ValidationRules => new() {
            new ValidateElementHasValue("brand", "Unisend"),
            new ValidateElementHasValue("operator", "Unisend", "uDrop"),
        };
}