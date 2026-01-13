namespace Osmalyzer;

[UsedImplicitly]
public class StatisticalRegionAnalyzer : AdminAnalyzerBase<AtvkEntry>
{
    public override string Name => "Statistical Regions";

    public override string Description => "This report checks statistical regions (statistiskie reģioni).";

    public override AnalyzerGroup Group => AnalyzerGroup.Administrative;


    public override List<Type> GetRequiredDataTypes() => [ 
        typeof(LatviaOsmAnalysisData), 
        typeof(AtvkAnalysisData),
        typeof(CspPopulationAnalysisData)
    ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmData OsmData = osmData.MasterData;

        OsmData osmAreas = OsmData.Filter(
            new IsRelation(),
            new OrMatch(
                new HasValue("boundary", "statistical"), // either stat region
                new AndMatch( // or country, i.e. latvia
                    new HasValue("boundary", "administrative"),
                    new HasValue("admin_level", "2")
                )
            ),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.CentroidInside)
        );
        
        // Find preset centers of boundaries (technically, just Latvia)

        SelfAssignAdminCenters(osmAreas.Relations, "place", "country");
        // note that Latvia has label and admin_center, but center is Riga, so that's not where the values go, it's actually label that duplicates country values 

        // Get all data sources

        AtvkAnalysisData atvkData = datas.OfType<AtvkAnalysisData>().First();
        
        CspPopulationAnalysisData cspData = datas.OfType<CspPopulationAnalysisData>().First();

        List<AtvkEntry> atvkAreas = atvkData.Entries.Where(e => e.Designation is AtvkDesignation.Region or AtvkDesignation.Country).ToList();
        if (atvkAreas.Count == 0) throw new Exception("No ATVK statistical area entries found.");
        
        // Assign CSP population data to ATVK regions, which we use as primary data items
        
        cspData.AssignToDataItems(
            atvkAreas,
            CspAreaType.Region,
            _ => null, // not doing lookups by name
            i => i.Code,
            _ => null // none should need it
        );
        
        cspData.AssignToDataItems(
            atvkAreas,
            CspAreaType.Country,
            i => i.Name, // expect "Latvija" basically
            _ => null, // not doing lookups by code
            _ => null // none should need it
        );
        
        // Prepare data comparer/correlator

        Correlator<AtvkEntry> correlator = new Correlator<AtvkEntry>(
            osmAreas,
            atvkAreas,
            new MatchAnywhereParamater(),
            // TODO: where can I get coords for these? neither csp nor atvk have them
            // new MatchDistanceParamater(25000),
            // new MatchFarDistanceParamater(75000),
            new MatchCallbackParameter<AtvkEntry>(GetRegionMatchStrength),
            new OsmElementPreviewValue("name", false),
            new DataItemLabelsParamater("statistical area", "statistical areas"),
            new LoneElementAllowanceParameter(DoesOsmElementLookLikeRegion)
        );

        [Pure]
        MatchStrength GetRegionMatchStrength(AtvkEntry entry, OsmElement osmElement)
        {
            if (entry.Designation == AtvkDesignation.Region)
            {
                string? refNum = osmElement.GetValue("ref");
                if (refNum == entry.Code)
                    return MatchStrength.Strong; // exact match on code

                string? name = osmElement.GetValue("name");
                if (name != null && entry.Name.StartsWith(name)) // e.g. "Latgale" vs "Latgales statistiskais reģions"
                    return MatchStrength.Strong; // great match on name

                // todo: alt name

                return MatchStrength.Unmatched;
            }
            else // else country
            {
                string? name = osmElement.GetValue("name");
                return name == "Latvija" ? MatchStrength.Strong : MatchStrength.Unmatched;
            }
        }

        [Pure]
        bool DoesOsmElementLookLikeRegion(OsmElement element)
        {
            string? boundary = element.GetValue("boundary");
            return boundary == "statistical";
            
            // we can't have lone country though, it has to match
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport correlatorReport = correlator.Parse(
            report, 
            new MatchedPairBatch(),
            new MatchedLoneOsmBatch(true)
            //new UnmatchedItemBatch(),
            //new MatchedFarPairBatch()
        );

        // Validate municipality syntax
        
        Validator<AtvkEntry> municipalityValidator = new Validator<AtvkEntry>(
            correlatorReport,
            "Region syntax issues"
        );

        List<SuggestedAction> suggestedChanges = municipalityValidator.Validate(
            report,
            false, false,
            // Always on relation
            new ValidateElementValueMatchesDataItemValue<AtvkEntry>("population", c => c.CspPopulationEntry?.Population.ToString()),
            new ValidateElementValueMatchesDataItemValue<AtvkEntry>("source:population", c => c.CspPopulationEntry?.Source),
            new ValidateElementValueMatchesDataItemValue<AtvkEntry>("population:date", c => c.CspPopulationEntry?.Year.ToString()),
            // Stat region-specific values
            new ValidateElementValueMatchesDataItemValue<AtvkEntry>(e => e.HasValue("boundary", "statistical"), "name", c => c.Name), // i.e. "Latgale"
            new ValidateElementValueMatchesDataItemValue<AtvkEntry>(e => e.HasValue("boundary", "statistical"), "alt_name", c => c.CspPopulationEntry?.Name), // i.e. "Latgales statistiskais reģions"
            new ValidateElementHasValue(e => e.HasValue("boundary", "statistical"), "designation", "statistiskais reģions"),
            // Also a copy on node, if present (basically, Latvia)
            new ValidateElementValueMatchesDataItemValue<AtvkEntry>(e => e.UserData != null, e => (OsmElement)e.UserData!, "population", c => c.CspPopulationEntry?.Population.ToString()),
            new ValidateElementValueMatchesDataItemValue<AtvkEntry>(e => e.UserData != null, e => (OsmElement)e.UserData!, "source:population", c => c.CspPopulationEntry?.Source),
            new ValidateElementValueMatchesDataItemValue<AtvkEntry>(e => e.UserData != null, e => (OsmElement)e.UserData!, "population:date", c => c.CspPopulationEntry?.Year.ToString())
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(OsmData, suggestedChanges, this);
        SuggestedActionApplicator.ExplainForReport(suggestedChanges, report, ExtraReportGroup.ProposedChanges);
#endif
    }


    private enum ExtraReportGroup
    {
        ProposedChanges
    }
}