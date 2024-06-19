using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

public abstract class ParcelLockerAnalyzer<T> : Analyzer where T : IParcelLockerListProvider
{
    public override string Name => Operator + " Parcel lockers"; // todo: and pickup points if so

    public override string Description => "This report checks that all " + Operator + " parcel lockers listed on company's website are found on the map." + Environment.NewLine +
                                          "Note that parcel locker websites can and do have errors: mainly incorrect position, but sometimes lockers are missing too.";

    public override AnalyzerGroup Group => AnalyzerGroups.ParcelLocker;


    protected abstract string Operator { get; }

    protected abstract List<ValidationRule>? LockerValidationRules { get; }
    
    protected abstract List<ValidationRule>? PickupPointValidationRules { get; }


    public override List<Type> GetRequiredDataTypes() => new List<Type>()
    {
        typeof(OsmAnalysisData),
        typeof(ParcelLockerOperatorAnalysisData), 
        typeof(T)
    };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Get relevant brand name (variations)

        ParcelLockerOperatorAnalysisData operatorData = datas.OfType<ParcelLockerOperatorAnalysisData>().First();
        List<string> brandNames = operatorData.Branding[Operator];

        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
                
        OsmDataExtract brandLockers = osmMasterData.Filter(
            new HasAnyValue("amenity", "parcel_locker"),
            new CustomMatch(LockerMatchesBrand)
        );
        // Note that we are only matching to brand-tagged lockers, because data coordinates are very approximate,
        // but parcel lockers are frequently found nearby to other lockers and we have no way to match like that 
        // because any of the other branded lockers could equally match - this isn't a "match" as far as we are concerned.
        // See UnknownParcelLockerAnalyzer for all the ones we don't explicitly match.
        
        bool LockerMatchesBrand(OsmElement osmElement)
        {
            // todo: use known brand data (file)

            string? osmName = osmElement.GetValue("name");

            if (osmName != null && brandNames.Exists(sn => osmName.ToLower().Contains(sn.ToLower())))
                return true;

            string? osmOperator = osmElement.GetValue("operator");

            if (osmOperator != null && brandNames.Exists(sn => osmOperator.ToLower().Contains(sn.ToLower())))
                return true;

            string? osmBrand = osmElement.GetValue("brand");

            if (osmBrand != null && brandNames.Exists(sn => osmBrand.ToLower().Contains(sn.ToLower())))
                return true;
            
            return false;
        }

        // Load data items
        IParcelLockerListProvider pointData = datas.OfType<IParcelLockerListProvider>().First();
        List<ParcelLocker> listedLockers = pointData.ParcelLockers.ToList();
        List<ParcelPickupPoint>? listedPickupPoints = pointData.PickupPoints?.ToList();

        
        // Lockers
        {

            // Prepare data comparer/correlator

            Correlator<ParcelLocker> correlator = new Correlator<ParcelLocker>(
                brandLockers,
                listedLockers,
                new MatchDistanceParamater(100),
                new MatchFarDistanceParamater(200),
                new MatchExtraDistanceParamater(MatchStrength.Strong, 500),
                new DataItemLabelsParamater(Operator + " parcel locker", Operator + " parcel lockers"),
                new OsmElementPreviewValue("name", false),
                new MatchCallbackParameter<ParcelLocker>(GetMatchStrength)
            );

            [Pure]
            MatchStrength GetMatchStrength(ParcelLocker point, OsmElement element)
            {
                if (point.Address != null)
                    if (FuzzyAddressMatcher.Matches(element, point.Address))
                        return MatchStrength.Strong;

                return MatchStrength.Good;
            }

            // Parse and report primary matching and location correlation

            CorrelatorReport correlatorReport = correlator.Parse(
                report,
                new MatchedPairBatch(),
                new MatchedLoneOsmBatch(true),
                new UnmatchedItemBatch(),
                new MatchedFarPairBatch(),
                new UnmatchedOsmBatch()
            );

            // Validate tagging

            Validator<ParcelLocker> validator = new Validator<ParcelLocker>(
                correlatorReport,
                listedPickupPoints != null ? "Parcel locker tagging issues" : "Tagging issues"
            );

            List<ValidationRule> rules = new List<ValidationRule>
            {
                new ValidateElementFixme()
            };

            // Add custom rules per operator/analyzer
            if (LockerValidationRules != null)
                rules.AddRange(LockerValidationRules);

            validator.Validate(report, rules.ToArray());
        }


        // Pickup points
        if (listedPickupPoints != null)
        {
            OsmDataExtract potentialAmenities;

            switch (pointData.PickupPointLocation)
            {
                case PickupPointAmenity.GasStation:
                    potentialAmenities = osmMasterData.Filter(
                        new OrMatch(
                            new HasValue("shop", "convenience"), // shop in fuel station mapped separately
                            new HasValue("amenity", "fuel")
                        ),
                        new HasValue("name", pointData.PickupPointLocationName!)
                    );
                    break;
                
                case PickupPointAmenity.Kiosk: 
                    potentialAmenities = osmMasterData.Filter(
                        new HasValue("shop", "kiosk"),
                        new HasValue("name", pointData.PickupPointLocationName!)
                    );
                    break;
                
                default:
                    throw new NotImplementedException();
            }
            
            // Prepare data comparer/correlator

            Correlator<ParcelPickupPoint> correlator = new Correlator<ParcelPickupPoint>(
                potentialAmenities,
                listedPickupPoints,
                new MatchDistanceParamater(100),
                new MatchFarDistanceParamater(200),
                new MatchExtraDistanceParamater(MatchStrength.Strong, 500),
                new DataItemLabelsParamater(Operator + " parcel pickup point", Operator + " parcel pickup points"),
                new OsmElementPreviewValue("name", false),
                new MatchCallbackParameter<ParcelPickupPoint>(GetMatchStrength),
                new LoneElementAllowanceParameter(IsOsmElementAllowedByItself)
            );

            [Pure]
            MatchStrength GetMatchStrength(ParcelPickupPoint point, OsmElement element)
            {
                string[]? serviceProviders = element.GetDelimitedValues("post_office:service_provider");

                if (serviceProviders != null)
                    if (!serviceProviders.Contains(Operator))
                        return MatchStrength.Unmatched; // this is some other providere's location (we could ALSO be here, but it's not mapped, so cannot easily assume to match)

                if (point.Address != null)
                    if (FuzzyAddressMatcher.Matches(element, point.Address))
                        return MatchStrength.Strong;

                return MatchStrength.Good;
            }

            [Pure]
            bool IsOsmElementAllowedByItself(OsmElement element)
            {
                if (element.HasValue("post_office", "post_partner"))
                    if (element.HasDelimitedValue("post_office:service_provider", Operator))
                        return true;

                return false;
            }

            // Parse and report primary matching and location correlation

            CorrelatorReport correlatorReport = correlator.Parse(
                report,
                new MatchedPairBatch(),
                new MatchedLoneOsmBatch(true),
                new UnmatchedItemBatch(),
                new MatchedFarPairBatch(),
                new MatchedLoneOsmBatch(true)
            );

            // Validate tagging

            Validator<ParcelPickupPoint> validator = new Validator<ParcelPickupPoint>(
                correlatorReport,
                "Pickup point tagging issues"
            );

            List<ValidationRule> rules = new List<ValidationRule>
            {
                new ValidateElementFixme()
            };

            // Add custom rules per operator/analyzer
            if (PickupPointValidationRules != null)
                rules.AddRange(PickupPointValidationRules);

            validator.Validate(report, rules.ToArray());
        }
    }
}