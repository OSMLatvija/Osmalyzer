namespace Osmalyzer;

[UsedImplicitly]
public class StreetTaggingContinuationAnalyzer : Analyzer
{
    public override string Name => "Street Tagging Continuation";

    public override string Description => "This report checks that streets with certain tags that apply to the whole street are applied consistently.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract ways = osmMasterData.Filter(
            new IsWay(),
            new HasAnyValue("highway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "living_street", "service", "track", "trunk_link", "primary_link", "secondary_link")
        );

        OsmDataExtract roadRoutes = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("type", "route"),
            new HasValue("route", "road"),
            new DoesntHaveKey("network"), // these are: lv:local, lv:regional, lv:national, e-road -- none of them are useful for minor city roads 
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy) // too many OOB hits
        );

        // Prepare groups

        report.AddGroup(ReportGroup.Problematic, "Inconsistent street tagging");

        report.AddEntry(
            ReportGroup.Problematic,
            new DescriptionReportEntry(
                "Issues."
            )
        );
        
        // Parse
        
        List<Street> streets = CollectStreets(ways, roadRoutes);
        
        List<ProblematicStreet> problematicStreets = new List<ProblematicStreet>();

        string[] consistentTags =
        {
            "name",
            "name:etymology",
            "name:etymology:wikipedia",
            "name:etymology:wikidata",
            "wikidata",
            "wikipedia"
        };
        
        foreach (Street street in streets)
        {
            List<StreetIssue> issues = new List<StreetIssue>();
            
            foreach (string tag in consistentTags)
            {
                List<string?> values = CollectValues(street, tag);

                if (values.Count > 1)
                    issues.Add(new MultipleValueIssue(tag, values));
            }
            
            if (issues.Count > 0)
                problematicStreets.Add(new ProblematicStreet(street, issues));
        }
        

        if (problematicStreets.Count > 0)
        {
            foreach (ProblematicStreet problematicStreet in problematicStreets)
            {
                report.AddEntry(
                    ReportGroup.Problematic,
                    new IssueReportEntry(
                        "Street/road " + (problematicStreet.Street.Route.HasKey("name") ? "`" + problematicStreet.Street.Route.GetValue("name") + "`" : "unnamed") + " " + problematicStreet.Street.Route.OsmViewUrl + 
                        " has " + ProblemDescription(problematicStreet) + ".",
                        problematicStreet.Street.Route.GetAverageCoord(),
                        MapPointStyle.Problem
                    )
                );

                [Pure]
                static string ProblemDescription(ProblematicStreet problematicStreet)
                {
                    return string.Join("; ", problematicStreet.Issues.Select(IssueDescription));


                    [Pure]
                    static string IssueDescription(StreetIssue streetIssue)
                    {
                        switch (streetIssue)
                        {
                            case MultipleValueIssue mvi:
                                return "multiple values for `" + mvi.Key + "`: " + string.Join(", ", mvi.Values.Select(v => v == null ? "empty" : "`" + v + "`"));
                                // todo: which segments?

                            default:
                                throw new ArgumentOutOfRangeException(nameof(streetIssue));
                        }
                    }
                }
            }
        }
    }

    [Pure]
    private static List<Street> CollectStreets(OsmDataExtract ways, OsmDataExtract roadRoutes)
    {
        List<Street> streets = new List<Street>();

        // First, collect streets from road route relations - this can include segments that are not physically connected
        // If routes are somehow wrong, then this will lead to value mismatches, which will get reported

        // Quick lookup whether ways are considered
        HashSet<long> allWays = ways.Elements.Select(e => e.Id).ToHashSet();

        Dictionary<long, RoadSegment> assignedWays = new Dictionary<long, RoadSegment>();
        
        foreach (OsmRelation roadRoute in roadRoutes.Relations)
        {
            List<RoadSegment> goodWays = new List<RoadSegment>();
            
            foreach (OsmElement roadElement in roadRoute.Elements)
            {
                if (roadElement is OsmWay roadWay)
                {
                    if (allWays.Contains(roadWay.Id))
                    {
                        if (assignedWays.TryGetValue(roadWay.Id, out RoadSegment? existingSegment))
                        {
                            existingSegment.RoadRoutes.Add(roadRoute);
                            
                            goodWays.Add(existingSegment);
                        }
                        else
                        {
                            RoadSegment roadSegment = new RoadSegment(roadWay, new List<OsmRelation>() { roadRoute });

                            goodWays.Add(roadSegment);

                            assignedWays.Add(roadWay.Id, roadSegment);
                        }
                    }
                }
                
                // todo: report incompatible ways?
            }
            
            if (goodWays.Count > 0)
                streets.Add(new Street(roadRoute, goodWays));
        }

        
        // TODO: grow
        
        
        // TODO: way by way
        
        
        // foreach (OsmWay way in ways.Ways)
        // {
        //     List<RoadRoute> routes = GetRoadRouteRelations(way);
        //     
        //     
        // }

        return streets;
    }

    private List<string?> CollectValues(Street street, string tag)
    {
        List<string?> values = new List<string?>();
        
        foreach (RoadSegment segment in street.Segments)
        {
            if (segment.RoadRoutes.Count > 1)
                continue; // multiple road routes - we don't need to bother checking these, they will always not have values for at least one street

            string? value = segment.Way.GetValue(tag);
            
            if (!values.Contains(value))
                values.Add(value);
        }

        values.Sort();
        
        return values;
    }


    private record RoadSegment(OsmWay Way, List<OsmRelation> RoadRoutes);
    
    private record Street(OsmRelation Route, List<RoadSegment> Segments);
    

    private record MultipleValueIssue(string Key, List<string?> Values) : StreetIssue;
    
    private abstract record StreetIssue;
    
    
    private record ProblematicStreet(Street Street, List<StreetIssue> Issues);

        
    private enum ReportGroup
    {
        Problematic
    }
}