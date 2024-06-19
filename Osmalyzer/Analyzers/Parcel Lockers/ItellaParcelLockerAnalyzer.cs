using System.Collections.Generic;

namespace Osmalyzer;

public class ItellaParcelLockerAnalyzer : ParcelLockerAnalyzer<ItellaParcelLockerAnalysisData>
{
    protected override string Operator => "Itella";

    protected override List<ValidationRule> LockerValidationRules => new List<ValidationRule>
    {
        new ValidateElementHasValue("brand", "Smartpost"), // Itella - operator, Smartpost - brand
        new ValidateElementHasValue("operator", Operator),
    };
    
    protected override List<ValidationRule>? PickupPointValidationRules => null; // TODO: !!!!!!!!!!!!!!!!!!!
}