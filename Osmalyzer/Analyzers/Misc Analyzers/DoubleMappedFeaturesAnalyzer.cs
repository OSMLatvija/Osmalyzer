namespace Osmalyzer;

[UsedImplicitly]
public class DoubleMappedFeaturesAnalyzer : Analyzer
{
    public override string Name => "Double-Mapped Features";

    public override string Description => "This report finds features that appear to be mapped double (or more), that is, redundantly, and should likely be combined or trimmed.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract areas = osmMasterData.Filter(
            new IsClosedWay(),
            new HasAnyKey(),
            new CustomMatch(OsmKnowledge.IsAreaFeature)
        );
        
        // TODO: MULTIPOLYGONS

        OsmDataExtract nodes = osmMasterData.Filter(
            new IsNode(),
            new HasAnyKey(),
            new CustomMatch(OsmKnowledge.IsAreaFeature)
        );

        // Prepare groups

        report.AddGroup(ReportGroup.NodeOverArea, "Nodes over areas");

        report.AddEntry(
            ReportGroup.NodeOverArea,
            new DescriptionReportEntry(
                "These POI/node features are mapped on top of the same type of area feature."
            )
        );
        
        // Parse

        List<RedundantFeature> redundantFeatures = new List<RedundantFeature>();


        foreach (OsmWay area in areas.Ways)
        {
            RedundantFeature? redundantFeature = null;
            
            foreach (OsmNode node in nodes.Nodes)
            {
                if (OsmKnowledge.AreSameAreaFeatures(area, node))
                {
                    if (area.ContainsCoord(node.coord))
                    {
                        if (redundantFeature == null)
                            redundantFeature = new RedundantFeature(area, new List<OsmNode>() { node });
                        else
                            redundantFeature.Nodes.Add(node);
                    }
                }
            }

            if (redundantFeature != null)
                redundantFeatures.Add(redundantFeature);
        }
        

        if (redundantFeatures.Count > 0)
        {
            foreach (RedundantFeature redundantFeature in redundantFeatures)
            {
                report.AddEntry(
                    ReportGroup.NodeOverArea,
                    new IssueReportEntry(
                        OsmKnowledge.GetFeatureLabel(redundantFeature.Area, "Area", true) + " " + redundantFeature.Area.OsmViewUrl + " has same feature node" + (redundantFeature.Nodes.Count > 1 ? "s" : "") + " on top of it: " +
                        string.Join("; ", redundantFeature.Nodes.Select(n => n.OsmViewUrl)),
                        redundantFeature.Nodes.Count == 1 ? redundantFeature.Nodes[0].coord : redundantFeature.Area.GetAverageCoord(),
                        MapPointStyle.Problem
                    )
                );
            }
        }
        else
        {
            report.AddEntry(
                ReportGroup.NodeOverArea,
                new GenericReportEntry(
                    "No redundant nodes on top of areas found."
                )
            );
        }
    }


    private record RedundantFeature(OsmWay Area, List<OsmNode> Nodes);
    
    
    private enum ReportGroup
    {
        NodeOverArea
    }
}