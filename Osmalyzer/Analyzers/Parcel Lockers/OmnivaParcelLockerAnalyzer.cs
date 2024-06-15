using System.Collections.Generic;

namespace Osmalyzer;

public class OmnivaParcelLockerAnalyzer : ParcelLockerAnalyzer<OmnivaParcelLockerAnalysisData>
{
    protected override string Operator => "Omniva";

    protected override List<ValidationRule> ValidationRules => new List<ValidationRule>
    {
        new ValidateElementHasValue("brand", Operator),
        new ValidateElementHasValue("brand:wikidata", "Q282457"),
        new ValidateElementHasValue("parcel_pickup", "yes"),
        new ValidateElementHasValue("parcel_mail_in", "yes"),
    };
}