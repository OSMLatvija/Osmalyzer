using System.Collections.Generic;

namespace Osmalyzer;

[UsedImplicitly]
public class ItellaParcelLockerAnalyzer : ParcelLockerAnalyzer<ItellaParcelLockerAnalysisData>
{
    protected override string Operator => "Itella";

    protected override List<ValidationRule> LockerValidationRules => new List<ValidationRule>
    {
        new ValidateElementHasValue("brand", "Smartpost"), // Itella - operator, Smartpost - brand
        new ValidateElementHasValue("operator", Operator),
    };

    protected override List<ValidationRule> PickupPointValidationRules => new List<ValidationRule>()
    {
        new ValidateElementHasValue("post_office:service_provider", Operator),
        //new ValidateElementHasValue("post_office:service_provider:wikidata", "???"),
        new ValidateElementHasValue("parcel_pickup", Operator),
        new ValidateElementHasValue("parcel_to", Operator),
        //new ValidateElementHasValue("parcel_from", "no")
    };
}