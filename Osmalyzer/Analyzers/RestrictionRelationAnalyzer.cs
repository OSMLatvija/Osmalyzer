using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class RestrictionRelationAnalyzer : Analyzer
{
    public override string Name => "Turn Restriction Relations";

    public override string Description => "This report checks turn restriction relations.";

    public override AnalyzerGroup Group => AnalyzerGroup.Validation;

    
    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
    

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data
        
        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmDataExtract restrictionRelations = osmData.MasterData.Filter(
            new IsRelation(),
            new HasValue("type", "restriction")
        );
        
        // Parse

        report.AddGroup(
            ReportGroup.RestrictionRelations, 
            "Restriction Relations"
        );
        
        report.AddEntry(
            ReportGroup.RestrictionRelations, 
            new DescriptionReportEntry($"Found {restrictionRelations.Relations.Count} restriction relations.")
        );
        
        // TODO
    }
    
    
    private enum ReportGroup
    {
        RestrictionRelations
    }
}
