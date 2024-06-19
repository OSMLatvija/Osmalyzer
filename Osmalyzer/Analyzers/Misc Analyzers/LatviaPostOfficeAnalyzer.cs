using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class LatviaPostOfficeAnalyzer : Analyzer
{
    protected string Operator { get; } = "Latvijas Pasts";

    public override string Name => Operator + " Post offices";

    public override string Description => "This report checks that all " + Operator + " post offices listed on company's website are found on the map." + Environment.NewLine +
                                          "Note that Latvijas pasts' website can and does have errors: mainly incorrect positions, but sometimes missing or phantom items too.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;

    public override List<Type> GetRequiredDataTypes() => new List<Type>()
    {
        typeof(OsmAnalysisData),
        typeof(LatviaPostAnalysisData),
        typeof(PostOfficeOperatorAnalysisData)
    };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
                
        OsmDataExtract osmPostBoxes = osmMasterData.Filter(
            new HasAnyValue("amenity", "post_office"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy) // a couple OOB hits
        );

        // Load post office data
        List<LatviaPostItem> listedBoxes = datas.OfType<LatviaPostAnalysisData>().First()
                                                .LatviaPostItems
                                                .Where(i => i.ItemType == LatviaPostItemType.Office).ToList();
        
        // Load other operators that can be slipped
        List<string> otherOperators = datas.OfType<PostOfficeOperatorAnalysisData>().First().Operators;

        // Prepare data comparer/correlator

        Correlator<LatviaPostItem> correlator = new Correlator<LatviaPostItem>(
            osmPostBoxes,
            listedBoxes,
            new MatchDistanceParamater(100),
            new MatchFarDistanceParamater(200),
            new MatchExtraDistanceParamater(MatchStrength.Strong, 500),
            new DataItemLabelsParamater(Operator + " post office", Operator + " post offices"),
            new OsmElementPreviewValue("name", false),
            new MatchCallbackParameter<LatviaPostItem>(GetMatchStrength),
            new LoneElementAllowanceParameter(e => !DoesElementLookLikeAnotherOperatorsPostOffice(e))
        );
        
        [Pure]
        MatchStrength GetMatchStrength(LatviaPostItem point, OsmElement element)
        {
            if (DoesElementLookLikeAnotherOperatorsPostOffice(element))
                return MatchStrength.Unmatched;
            
            if (point.Address != null)
                if (FuzzyAddressMatcher.Matches(element, point.Address))
                    return MatchStrength.Strong;
                
            return MatchStrength.Good;
        }
        
        [Pure]
        bool DoesElementLookLikeAnotherOperatorsPostOffice(OsmElement element)
        {
            if (element.HasValue("operator", otherOperators, false))
                return true;
            
            if (element.HasValue("brand", otherOperators, false))
                return true;
            
            return false;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport correlatorReport = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch()
        );

        // Validate tagging

        Validator<LatviaPostItem> validator = new Validator<LatviaPostItem>(
            correlatorReport,
            "Tagging issues"
        );

        validator.Validate(
            report,
            false, // there are non-LP post offices, so we don't check against them
            new ValidateElementValueMatchesDataItemValue<LatviaPostItem>("name", di => di.Name),
            new ValidateElementHasValue("operator", Operator),
            new ValidateElementHasValue("operator:wikidata", "Q1807088"),
            new ValidateElementFixme()
        );

        // Stats

        // report.AddGroup(ReportGroup.Stats, "Stats");
        //
        // report.AddEntry(
        //     ReportGroup.Stats,
        //     new DescriptionReportEntry(
        //         "Names: " + string.Join(", ", foundValues.OrderByDescending(v => v.Value).Select(kv => "`" + kv.Key + "` ×" + kv.Value + ""))
        //     )
        // );
    }
    
    
    private enum ReportGroup
    {
        Stats
    }
}