using System.Collections.Generic;

namespace Osmalyzer;

public class ItellaParcelLockerAnalyzer : ParcelLockerAnalyzer<ItellaParcelLockerAnalysisData>
{
    protected override string Operator => "Itella";

    protected override List<ValidationRule>? ValidationRules => new() {
            new ValidateElementHasValue("brand", "Smartpost"), //Itella - operator, Smartpost - brand
            new ValidateElementHasValue("operator", Operator),
        };
}