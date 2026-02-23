namespace Osmalyzer;

[UsedImplicitly]
public class DoubleMappedFeaturesAnalyzer : Analyzer
{
    public override string Name => "Double-Mapped Features";

    public override string Description => "This report finds features that appear to be mapped double (or more), that is, redundantly, and should likely be combined or trimmed.";

    public override AnalyzerGroup Group => AnalyzerGroup.Validation;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData OsmData = osmData.MasterData;

        OsmData areas = OsmData.Filter(
            new IsClosedWay(),
            new HasAnyKey(),
            new CustomMatch(OsmKnowledge.IsAreaFeature)
        );
        
        // TODO: MULTIPOLYGONS

        OsmData nodes = OsmData.Filter(
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

        List<RedundantFeature> redundantFeatures = [ ];


        foreach (OsmWay area in areas.Ways)
        {
            if (!IncludeArea(area))
                continue;
            
            RedundantFeature? redundantFeature = null;
            
            foreach (OsmNode node in nodes.Nodes)
            {
                // Ignore distant features, about 1 km
                if (OsmGeoTools.DistanceBetweenCheap(area.AverageCoord, node.AverageCoord) > 1000) 
                    continue;

                if (OsmKnowledge.AreSameAreaFeatures(area, node))
                {
                    if (area.ContainsCoord(node.coord))
                    {
                        if (redundantFeature == null)
                            redundantFeature = new RedundantFeature(area, [ node ], OsmKnowledge.GetFeatureLabel(area, "area", false));
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
            foreach (RedundantFeature redundantFeature in redundantFeatures.OrderBy(rf => rf.Descriptor))
            {
                report.AddEntry(
                    ReportGroup.NodeOverArea,
                    new IssueReportEntry(
                        OsmKnowledge.GetFeatureLabel(redundantFeature.Area, "Area", true) + " (" + OsmGeoTools.GetAreaSize(redundantFeature.Area).ToString("F3") + "km2) " + redundantFeature.Area.OsmViewUrl + " has same feature node" + (redundantFeature.Nodes.Count > 1 ? "s" : "") + " on top of it: " +
                        string.Join("; ", redundantFeature.Nodes.Select(n => n.OsmViewUrl)),
                        redundantFeature.Nodes.Count == 1 ? redundantFeature.Nodes[0].coord : redundantFeature.Area.AverageCoord,
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

    [Pure]
    private static bool IncludeArea(OsmWay area)
    {
        // Ignore huge areas
        if (OsmGeoTools.GetAreaSize(area) > 0.3) // km^2
            return false;
        
        // Ignore isolated dwellings, there is not yet consensus on how to treat these being double mapped - one with node and name, one with area for landuse layout
        if (area.HasValue("place", "isolated_dwelling"))
            return false;
        
        return true;
    }


    private record RedundantFeature(OsmWay Area, List<OsmNode> Nodes, string Descriptor);
    
    
    private enum ReportGroup
    {
        NodeOverArea
    }
}