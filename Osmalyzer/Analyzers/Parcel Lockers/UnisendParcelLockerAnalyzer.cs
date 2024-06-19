using System.Collections.Generic;

namespace Osmalyzer;

public class UnisendParcelLockerAnalyzer : ParcelLockerAnalyzer<UnisendParcelLockerAnalysisData>
{
    protected override string Operator => "Unisend";

    protected override List<ValidationRule> LockerValidationRules => new List<ValidationRule>
    {
        new ValidateElementHasValue("brand", "Unisend"),
        new ValidateElementHasValue("operator", "Unisend", "uDrop"),
    };

    protected override List<ValidationRule>? PickupPointValidationRules => null; // we don't have any pickup points
}