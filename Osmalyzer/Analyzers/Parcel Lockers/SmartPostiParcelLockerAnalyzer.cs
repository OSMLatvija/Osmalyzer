namespace Osmalyzer;

[UsedImplicitly]
public class SmartPostiParcelLockerAnalyzer : ParcelLockerAnalyzer<ItellaParcelLockerAnalysisData>
{
    protected override string Operator => "SmartPosti";

    protected override List<ValidationRule> LockerValidationRules => new List<ValidationRule>
    {
        new ValidateElementHasValue("brand", Operator), // used to be Itella - operator, Smartpost - brand
        new ValidateElementHasValue("brand:wikidata", "Q132157239"),
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