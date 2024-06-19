using System.Collections.Generic;

namespace Osmalyzer;

public class VenipakParcelLockerAnalyzer : ParcelLockerAnalyzer<VenipakParcelLockerAnalysisData>
{
    protected override string Operator => "Venipak";

    protected override List<ValidationRule> LockerValidationRules => new List<ValidationRule>
    {
        new ValidateElementHasValue("brand", Operator),
        new ValidateElementHasValue("brand:wikidata", "Q124379827"),
        new ValidateElementHasValue("parcel_pickup", "yes"),
        new ValidateElementHasValue("parcel_mail_in", "yes"),
    };

    protected override List<ValidationRule>? PickupPointValidationRules => null; // we don't have any pickup points
}