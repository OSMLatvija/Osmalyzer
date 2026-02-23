namespace Osmalyzer;

[UsedImplicitly]
public class PlaygroundAnalyzer : Analyzer
{
    public override string Name => "Playgrounds";

    public override string Description => "This report checks playground equipment mapping as part of a playground feature. " +
                                          "It reports errors when playground equipment is mapped without a nearby playground. " +
                                          "It also reports warnings when equipment is located outside the exact bounds of the playground. " +
                                          "Note that it is not necessarily an error if some playground equipment is not inside an actual designated playground.";

    public override AnalyzerGroup Group => AnalyzerGroup.Validation;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];


    /// <summary>
    /// Maximum distance in meters for equipment to be considered "near" a playground node.
    /// Used when the playground is mapped as a node rather than as an area.
    /// </summary>
    private const double maxNodeProximityDistance = 30;

    /// <summary>
    /// Maximum distance in meters to search for a playground when checking orphan equipment
    /// </summary>
    private const double maxPlaygroundSearchDistance = 100;


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmData masterData = osmData.MasterData;

        // Get all playgrounds (leisure=playground)
        OsmData playgrounds = masterData.Filter(
            new HasValue("leisure", "playground"),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(masterData), OsmPolygon.RelationInclusionCheck.FuzzyLoose)
        );

        // Get all playground equipment (playground=*)
        OsmData playgroundEquipment = masterData.Filter(
            new HasKey("playground"),
            new DoesntHaveValue("leisure", "playground"), // exclude elements that are ALSO tagged as playgrounds, likely error or some weird way to clarify playground type
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(masterData), OsmPolygon.RelationInclusionCheck.FuzzyLoose)
        );

        // Prepare report groups

        report.AddGroup(
            ReportGroup.OrphanEquipment,
            "Orphan Playground Equipment",
            "These playground equipment items are not associated with any playground area.",
            "All playground equipment is properly associated with a playground."
        );

        report.AddGroup(
            ReportGroup.EquipmentOutsideBounds,
            "Equipment Outside Playground Bounds",
            "These playground equipment items are located outside the bounds of their associated playground.",
            "All playground equipment is within playground bounds."
        );

        report.AddGroup(
            ReportGroup.Stats,
            "Statistics"
        );

        // Build lookup structures

        List<PlaygroundArea> playgroundAreas = BuildPlaygroundAreas(playgrounds);

        // Analyze each piece of equipment

        int orphanCount = 0;
        int outsideBoundsCount = 0;
        int validCount = 0;

        foreach (OsmElement equipment in playgroundEquipment.Elements)
        {
            string equipmentType = equipment.GetValue("playground") ?? "unknown";
            OsmCoord equipmentCoord = equipment.AverageCoord;

            // Find associated playground
            PlaygroundArea? containingPlayground = FindContainingPlayground(equipment, playgroundAreas);

            if (containingPlayground == null)
            {
                // Equipment is not inside any playground - check if there's a nearby playground node
                PlaygroundArea? nearbyPlayground = FindNearestPlayground(equipmentCoord, playgroundAreas, maxPlaygroundSearchDistance);

                if (nearbyPlayground == null)
                {
                    // No playground found at all - this is an error
                    report.AddEntry(
                        ReportGroup.OrphanEquipment,
                        new IssueReportEntry(
                            "Playground equipment `" + equipmentType + "` has no nearby/associated playground area - " + equipment.OsmViewUrl,
                            equipmentCoord,
                            MapPointStyle.Problem,
                            equipment
                        )
                    );
                    orphanCount++;
                }
                else if (nearbyPlayground.IsNode)
                {
                    // There's a nearby node playground - check distance
                    double distance = OsmGeoTools.DistanceBetween(equipmentCoord, nearbyPlayground.Element.AverageCoord);

                    if (distance > maxNodeProximityDistance)
                    {
                        report.AddEntry(
                            ReportGroup.EquipmentOutsideBounds,
                            new IssueReportEntry(
                                "Playground equipment `" + equipmentType + "` is " + distance.ToString("F0") + "m from nearby playground node - " + equipment.OsmViewUrl,
                                equipmentCoord,
                                MapPointStyle.Dubious,
                                equipment
                            )
                        );
                        outsideBoundsCount++;
                    }
                    else
                    {
                        validCount++;
                    }
                }
                else
                {
                    // There's a nearby area playground but equipment is outside it
                    report.AddEntry(
                        ReportGroup.EquipmentOutsideBounds,
                        new IssueReportEntry(
                            "Playground equipment `" + equipmentType + "` is outside nearby playground's bounds - " + equipment.OsmViewUrl + 
                            " ; " + nearbyPlayground.Element.OsmViewUrl,
                            equipmentCoord,
                            MapPointStyle.Dubious,
                            equipment
                        )
                    );
                    outsideBoundsCount++;
                }
            }
            else
            {
                validCount++;
            }
        }
        
        // Report way or relation playground with no detected polygon (likely broken)

        foreach (PlaygroundArea area in playgroundAreas)
        {
            if (!area.IsNode)
            {
                if (area.MultiPolygon == null)
                {
                    report.AddEntry(
                        ReportGroup.EquipmentOutsideBounds,
                        new IssueReportEntry(
                            "Playground has no detected expected polygon - " + area.Element.OsmViewUrl,
                            area.Element.AverageCoord,
                            MapPointStyle.Problem,
                            area.Element
                        )
                    );
                }
            }
        }

        // Report stats

        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                "Found " + playgrounds.Elements.Count + " playgrounds and " + playgroundEquipment.Elements.Count + " equipment items."
            )
        );

        report.AddEntry(
            ReportGroup.Stats,
            new GenericReportEntry(
                "Valid: " + validCount + ", Orphan: " + orphanCount + ", Outside bounds: " + outsideBoundsCount + "."
            )
        );
    }


    [Pure]
    private static List<PlaygroundArea> BuildPlaygroundAreas(OsmData playgrounds)
    {
        List<PlaygroundArea> areas = [ ];

        foreach (OsmElement element in playgrounds.Elements)
        {
            OsmMultiPolygon? multiPolygon = null;
            bool isNode = false;

            switch (element)
            {
                case OsmNode:
                    isNode = true;
                    break;

                case OsmWay way when way.Closed:
                    // Convert single way polygon to multipolygon format for unified handling
                    multiPolygon = new OsmMultiPolygon([ way.GetPolygon() ], [ ]);
                    break;

                case OsmRelation relation:
                    multiPolygon = relation.GetMultipolygon();
                    break;
            }

            areas.Add(new PlaygroundArea(element, multiPolygon, isNode));
            
            // Note that multiPolygon can be null for ways that are not closed or broken multipolygon relations - in that case, we will rely on proximity checks rather than polygon containment
        }

        return areas;
    }


    [Pure]
    private static PlaygroundArea? FindContainingPlayground(OsmElement equipment, List<PlaygroundArea> playgrounds)
    {
        OsmCoord coord = equipment.AverageCoord;

        foreach (PlaygroundArea playground in playgrounds)
        {
            if (playground.MultiPolygon != null && playground.MultiPolygon.ContainsCoord(coord))
                return playground;

            // For node playgrounds, check if equipment is very close
            if (playground.IsNode)
            {
                double distance = OsmGeoTools.DistanceBetween(coord, playground.Element.AverageCoord);
                if (distance <= maxNodeProximityDistance)
                    return playground;
            }
        }

        return null;
    }


    [Pure]
    private static PlaygroundArea? FindNearestPlayground(OsmCoord coord, List<PlaygroundArea> playgrounds, double maxDistance)
    {
        PlaygroundArea? nearest = null;
        double nearestDistance = double.MaxValue;

        foreach (PlaygroundArea playground in playgrounds)
        {
            double distance = OsmGeoTools.DistanceBetween(coord, playground.Element.AverageCoord);

            if (distance < nearestDistance && distance <= maxDistance)
            {
                nearestDistance = distance;
                nearest = playground;
            }
        }

        return nearest;
    }


    private record PlaygroundArea(OsmElement Element, OsmMultiPolygon? MultiPolygon, bool IsNode);


    private enum ReportGroup
    {
        OrphanEquipment,
        EquipmentOutsideBounds,
        Stats
    }
}

