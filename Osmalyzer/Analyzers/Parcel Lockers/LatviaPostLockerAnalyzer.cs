namespace Osmalyzer;

[UsedImplicitly]
public class LatviaPostLockerAnalyzer : ParcelLockerAnalyzer<LatviaPostAnalysisData>
{
    protected override string Operator => "Latvijas Pasts";


    private const string operatorWikidata = "Q1807088";

    
    protected override List<ValidationRule> LockerValidationRules =>
    [
        new ValidateElementHasValue("brand", Operator),
        new ValidateElementHasValue("brand:wikidata", operatorWikidata),
        // new ValidateElementHasValue("parcel_pickup", "yes"),
        // new ValidateElementHasValue("parcel_mail_in", "yes"),
        new ValidateElementValueMatchesDataItemValue<ParcelLocker>("indoor", pl => pl.Indoors == true ? "yes" : pl.Indoors == false ? "no" : null)
    ];

    protected override List<ValidationRule> PickupPointValidationRules =>
    [
        new ValidateElementHasValue("post_office:service_provider", Operator),
        new ValidateElementHasValue("post_office:service_provider:wikidata", operatorWikidata),
        new ValidateElementHasValue("parcel_pickup", Operator),
        new ValidateElementHasValue("parcel_to", Operator)
        //new ValidateElementHasValue("parcel_from", "no")
    ];
}