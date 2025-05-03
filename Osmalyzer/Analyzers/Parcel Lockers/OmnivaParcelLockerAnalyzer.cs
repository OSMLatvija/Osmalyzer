namespace Osmalyzer;

[UsedImplicitly]
public class OmnivaParcelLockerAnalyzer : ParcelLockerAnalyzer<OmnivaParcelLockerAnalysisData>
{
    protected override string Operator => "Omniva";

    protected override List<ValidationRule> LockerValidationRules => new List<ValidationRule>
    {
        new ValidateElementHasValue("brand", Operator),
        new ValidateElementHasValue("brand:wikidata", "Q282457"),
        new ValidateElementHasValue("parcel_pickup", "yes"),
        new ValidateElementHasValue("parcel_mail_in", "yes"),
    };

    protected override List<ValidationRule>? PickupPointValidationRules => null; // we don't have any pickup points
}