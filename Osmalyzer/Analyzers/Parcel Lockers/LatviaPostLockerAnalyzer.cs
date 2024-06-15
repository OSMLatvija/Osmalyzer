using System.Collections.Generic;

namespace Osmalyzer;

[UsedImplicitly]
public class LatviaPostLockerAnalyzer : ParcelLockerAnalyzer<LatviaPostAnalysisData>
{
    protected override string Operator => "Latvijas Pasts";

    protected override List<ValidationRule>? ValidationRules => new() {
            new ValidateElementHasValue("brand", Operator),
            new ValidateElementHasValue("brand:wikidata", "Q1807088"),
            // new ValidateElementHasValue("parcel_pickup", "yes"),
            // new ValidateElementHasValue("parcel_mail_in", "yes"),
        };
}