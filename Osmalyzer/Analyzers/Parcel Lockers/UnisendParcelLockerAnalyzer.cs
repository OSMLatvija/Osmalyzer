namespace Osmalyzer;

[UsedImplicitly]
public class UnisendParcelLockerAnalyzer : ParcelLockerAnalyzer<UnisendParcelLockerAnalysisData>
{
    protected override string Operator => "Unisend";

    protected override List<ValidationRule> LockerValidationRules => new List<ValidationRule>
    {
        new ValidateElementHasValue("brand", "Unisend"),
        new ValidateElementHasAnyValue("operator", "Unisend", "uDrop"),
    };

    protected override List<ValidationRule>? PickupPointValidationRules => null; // we don't have any pickup points
}