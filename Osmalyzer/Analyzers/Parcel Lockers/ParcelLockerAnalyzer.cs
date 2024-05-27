using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public abstract class ParcelLockerAnalyzer<T> : Analyzer where T : ParcelLockerAnalysisData
{
    public override string Name => Operator + " Parcel lockers";

    public override string Description => "This report checks that all " + Operator + " parcel lockers listed on company's website are found on the map." + Environment.NewLine +
                                          "Note that parcel locker websites can and do have errors.";

    public override AnalyzerGroup Group => AnalyzerGroups.ParcelLocker;


    protected abstract string Operator { get; }

    protected abstract List<string> ParcelLockerOsmNames { get; }


    public override List<Type> GetRequiredDataTypes() => new List<Type>()
    {
        typeof(OsmAnalysisData), 
        typeof(T)
    };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
                
        OsmDataExtract osmLockers = osmMasterData.Filter(
            new HasAnyValue("amenity", "parcel_locker")
        );
        
        OsmDataExtract brandLockers = osmLockers.Filter(
            new CustomMatch(LockerNameMatches)
        );

        bool LockerNameMatches(OsmElement osmElement)
        {
            // todo: use known brand data (file)

            string? osmName = osmElement.GetValue("name");

            if (osmName != null && ParcelLockerOsmNames.Exists(sn => osmName.ToLower().Contains(sn.ToLower())))
                return true;

            string? osmOperator = osmElement.GetValue("operator");

            if (osmOperator != null && ParcelLockerOsmNames.Exists(sn => osmOperator.ToLower().Contains(sn.ToLower())))
                return true;

            string? osmBrand = osmElement.GetValue("brand");

            if (osmBrand != null && ParcelLockerOsmNames.Exists(sn => osmBrand.ToLower().Contains(sn.ToLower())))
                return true;
            
            return false;
        }

        // Load Parcel locker data
        List<ParcelLocker> listedLockers = datas.OfType<ParcelLockerAnalysisData>().First().ParcelLockers.ToList();

        // Prepare data comparer/correlator

        Correlator<ParcelLocker> dataComparer = new Correlator<ParcelLocker>(
            brandLockers,
            listedLockers,
            new MatchDistanceParamater(100),
            new MatchFarDistanceParamater(200),
            new MatchExtraDistanceParamater(MatchStrength.Strong, 500),
            new DataItemLabelsParamater(Operator + " parcel locker", Operator + " parcel lockers"),
            new OsmElementPreviewValue("name", false),
            new LoneElementAllowanceCallbackParameter(_ => true),
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

        dataComparer.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new UnmatchedOsmBatch()
        );
    }
}