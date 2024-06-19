using System.Collections.Generic;

namespace Osmalyzer;

[UsedImplicitly]
public class LatviaPostLockerAnalyzer : ParcelLockerAnalyzer<LatviaPostAnalysisData>
{
    protected override string Operator => "Latvijas Pasts";

    protected override List<ValidationRule> LockerValidationRules => new List<ValidationRule>
    {
        new ValidateElementHasValue("brand", Operator),
        new ValidateElementHasValue("brand:wikidata", "Q1807088"),
        // new ValidateElementHasValue("parcel_pickup", "yes"),
        // new ValidateElementHasValue("parcel_mail_in", "yes"),
    };

    protected override List<ValidationRule> PickupPointValidationRules => new List<ValidationRule>()
    {
        new ValidateElementHasValue("post_office:service_provider", "Latvijas Pasts"),
        new ValidateElementHasValue("post_office:service_provider:wikidata", "Q1807088"),
        new ValidateElementHasValue("parcel_pickup", "Latvijas Pasts"),
        new ValidateElementHasValue("parcel_to", "Latvijas Pasts"),
        //new ValidateElementHasValue("parcel_from", "no")
    };
}