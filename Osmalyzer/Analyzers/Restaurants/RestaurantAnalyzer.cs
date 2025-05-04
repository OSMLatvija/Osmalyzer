using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class RestaurantAnalyzer<TRestaurant, TOsm> : Analyzer
    where TRestaurant : RestaurantListAnalysisData
    where TOsm : OsmAnalysisData
{
    public override string Name => RestaurantName + (RestaurantNameDisambiguator != null ? " (" + RestaurantNameDisambiguator + ")" : "") + " Restaurants";

    protected virtual string? RestaurantNameDisambiguator => null;

    public override string Description => "This report checks that all " + RestaurantName + " restaurants listed on brand's website are found on the map." + Environment.NewLine +
                                          "This supposes that brand restaurants are tagged correctly to match among multiple." + Environment.NewLine +
                                          "Note that websites can and do have errors, mainly large offsets, but also missing or incorrect locations.";

    public override AnalyzerGroup Group => AnalyzerGroups.Restaurants;


    protected abstract string RestaurantName { get; }

    protected abstract List<string> RestaurantOsmNames { get; }


    public override List<Type> GetRequiredDataTypes() => new List<Type>()
    {
        typeof(TOsm),
        typeof(TRestaurant) // restaurant list data
    };


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        TOsm osmData = datas.OfType<TOsm>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmRestaurants = osmMasterData.Filter(
            new HasAnyValue("amenity", "fast_food", "cafe")
        );

        OsmDataExtract brandRestaurants = osmRestaurants.Filter(
            new CustomMatch(RestaurantNameMatches)
        );

        bool RestaurantNameMatches(OsmElement osmElement)
        {
            // todo: use known brand data (file)

            string? osmName = osmElement.GetValue("name");

            if (osmName != null && RestaurantOsmNames.Any(sn => osmName.ToLower().Contains(sn.ToLower())))
                return true;

            string? osmOperator = osmElement.GetValue("operator");

            if (osmOperator != null && RestaurantOsmNames.Any(sn => osmOperator.ToLower().Contains(sn.ToLower())))
                return true;

            string? osmBrand = osmElement.GetValue("brand");

            if (osmBrand != null && RestaurantOsmNames.Any(sn => osmBrand.ToLower().Contains(sn.ToLower())))
                return true;

            return false;
        }

        // Load Restaurant data

        RestaurantListAnalysisData restaurantData = datas.OfType<RestaurantListAnalysisData>().First();

        List<RestaurantData> listedRestaurants = restaurantData.Restaurant.ToList();

        // Prepare data comparer/correlator

        Correlator<RestaurantData> correlator = new Correlator<RestaurantData>(
            brandRestaurants,
            listedRestaurants,
            new MatchDistanceParamater(100),
            new MatchFarDistanceParamater(300), // some are really far from where the data says they ought to be
            new MatchExtraDistanceParamater(MatchStrength.Strong, 700), // allow really far for exact matches
            new DataItemLabelsParamater(RestaurantName + " restaurant", RestaurantName + " restaurants"),
            new OsmElementPreviewValue("name", false),
            new MatchCallbackParameter<RestaurantData>(GetMatchStrength)
        );

        // todo: report closest potential (brand-untagged) restaurant when not matching anything?

        [Pure]
        MatchStrength GetMatchStrength(RestaurantData point, OsmElement element)
        {
            if (point.Address != null)
                if (FuzzyAddressMatcher.Matches(element, point.Address))
                    return MatchStrength.Strong;

            return MatchStrength.Good;
        }

        // Parse and report primary matching and location correlation

        correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new UnmatchedOsmBatch()
        );
    }
}