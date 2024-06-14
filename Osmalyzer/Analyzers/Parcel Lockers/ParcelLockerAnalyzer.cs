using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public abstract class ParcelLockerAnalyzer<T> : Analyzer where T : IParcelLockerListProvider
{
    public override string Name => Operator + " Parcel lockers";

    protected virtual List<ValidationRule>? ValidationRules => null;

    public override string Description => "This report checks that all " + Operator + " parcel lockers listed on company's website are found on the map." + Environment.NewLine +
                                          "Note that parcel locker websites can and do have errors: mainly incorrect position, but sometimes lockers are missing too.";

    public override AnalyzerGroup Group => AnalyzerGroups.ParcelLocker;


    protected abstract string Operator { get; }


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
                
        OsmDataExtract osmLockers = osmMasterData.Filter(
            new HasAnyValue("amenity", "parcel_locker")
        );

        OsmDataExtract brandLockers = osmLockers.Filter(
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

        // Load Parcel locker data
        List<ParcelLocker> listedLockers = datas.OfType<IParcelLockerListProvider>().First().ParcelLockers.ToList();

        // Prepare data comparer/correlator

        Correlator<ParcelLocker> dataComparer = new Correlator<ParcelLocker>(
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

        CorrelatorReport correlatorReport = dataComparer.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new UnmatchedOsmBatch()
        );

        //

        Validator<ParcelLocker> validator = new Validator<ParcelLocker>(
            correlatorReport,
            "Tagging issues"
        );

        List<ValidationRule> rules = new() {
            new ValidateElementFixme()
        };
        if (ValidationRules != null) 
            rules.AddRange(ValidationRules);

        validator.Validate(report, rules.ToArray());
    }
}