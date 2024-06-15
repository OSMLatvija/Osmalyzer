using System.Collections.Generic;

namespace Osmalyzer;

public class DPDParcelLockerAnalyzer : ParcelLockerAnalyzer<DPDParcelLockerAnalysisData>
{
    protected override string Operator => "DPD";

    protected override List<ValidationRule> ValidationRules => new List<ValidationRule>
    {
        new ValidateElementHasValue("brand", Operator),
        new ValidateElementHasValue("operator", "DPD Latvia"),
        new ValidateElementHasValue("operator:wikidata", "Q125973085"),
        new ValidateElementHasValue("brand:wikidata", "Q541030"),
        new ValidateElementHasValue("parcel_pickup", "yes"),
        new ValidateElementHasValue("parcel_mail_in", "yes"),
    };
}