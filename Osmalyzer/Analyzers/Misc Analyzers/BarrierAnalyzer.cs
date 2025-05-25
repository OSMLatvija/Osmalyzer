namespace Osmalyzer;

[UsedImplicitly]
public class BarrierAnalyzer : Analyzer
{
    public override string Name => "Barriers";

    public override string Description => "This report checks issues and completness of mapped barriers (gates, blocks, etc.).";

    public override AnalyzerGroup Group => AnalyzerGroup.Roads;

    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Barrier stuff
        
        // todo: to data file

        List<Barrier> barriers = [
            new Barrier("block", false), // often located by themselves in a row, along areas, etc.
            new Barrier("bollard", false), // often located by themselves in a row, dividing roads, traffic calming, etc.
            new Barrier("bump_gate", true),
            new Barrier("cattle_grid", true),
            new Barrier("coupure", true),
            new Barrier("cycle_barrier", true),
            new Barrier("debris", true),
            new Barrier("full-height_turnstile", true),
            new Barrier("gate", true),
            new Barrier("hampshire_gate", true),
            new Barrier("height_restrictor", true),
            new Barrier("horse_stile", true),
            new Barrier("kissing_gate", true),
            new Barrier("lift_gate", true),
            new Barrier("motorcycle_barrier", true),
            new Barrier("planter", false), // often located by themselves in a row, along areas, etc.
            new Barrier("sliding_beam", true),
            new Barrier("sliding_gate", true),
            new Barrier("spikes", true), 
            new Barrier("stile", true),
            new Barrier("sump_buster", true), 
            new Barrier("swing_gate", true),
            new Barrier("turnstile", true),
            new Barrier("wedge", true), 
            new Barrier("wicket_gate", true),
            new Barrier("chain", true),
            new Barrier("jersey_barrier", false), // usualyl along roads
            new Barrier("kerb", true),
            new Barrier("log", true),
            new Barrier("rope", true),
            new Barrier("tank_trap", false), // most likely located randomly somewhere rather than on actual ways
            new Barrier("tyres", false) // most likely located along tracks and such
        ];
        
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;
        
        OsmDataExtract osmCrossingNodes = osmMasterData.Filter(
            new IsNode(),
            new HasAnyValue("barrier", barriers.Select(b => b.OsmValue)),
            new InsidePolygon(BoundaryHelper.GetLatviaPolygon(osmData.MasterData), OsmPolygon.RelationInclusionCheck.Fuzzy)
        );
            
        // Parse

        List<SeenBarrier> seenBarriers = [ ];
        
        report.AddGroup(
            ReportGroup.NonWayBarriers,
            "Barriers not on Ways",
            "These barriers are not on any expected way. " +
            "Barriers as nodes should usually appear on routable ways (highways, railways), otherwise their use is limited. " +
            "It is not always incorrect to map them by themselves, just usually incomplete. " +
            "Note that barriers can and do appear by themselves, such as blocks and bollards that don't actually block any specific way." +
            "All barriers have an expected associated way."
        );
        
        report.AddGroup(
            ReportGroup.Stats,
            "Stats"
        );

        List<NonWayBarrierNode> nonWayBarriers = [ ];

        foreach (OsmNode node in osmCrossingNodes.Nodes)
        {
            bool isOnWay = false;

            if (node.Ways != null)
            {
                foreach (OsmWay parentWay in node.Ways)
                {
                    if (parentWay.HasValue(
                            "highway",
                            "motorway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential",
                            "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link",
                            "living_street", "pedestrian", "service", "track",
                            "footway", "path", "cycleway",
                            "platform"
                        ))
                    {
                        isOnWay = true;
                    }
                    else if (parentWay.HasValue(
                            "railway",
                            "rail", "tram",
                            "disused", "abandoned", "razed",
                            "platform"
                        ))
                    {
                        isOnWay = true;
                    }
                }
            }

            Barrier barrier = barriers.Single(b => b.OsmValue == node.GetValue("barrier"));

            SeenBarrier? seenBarrier = seenBarriers.FirstOrDefault(sb => sb.Barrier == barrier);

            if (seenBarrier != null)
                seenBarrier.Count++;
            else
                seenBarriers.Add(new SeenBarrier(barrier, 1));

            if (!isOnWay)
                nonWayBarriers.Add(new NonWayBarrierNode(barrier, node, barrier.MustBeOnWay));
        }

        foreach (NonWayBarrierNode nonWayBarrier in nonWayBarriers)
        {
            report.AddEntry(
                ReportGroup.NonWayBarriers,
                new IssueReportEntry(
                    "This `" + nonWayBarrier.Barrier.OsmValue + "` barrier is not on a way - " + nonWayBarrier.Node.OsmViewUrl,
                    nonWayBarrier.Node.GetAverageCoord(),
                    nonWayBarrier.Bad ? MapPointStyle.Problem : MapPointStyle.Dubious
                )
            );
        }
        
        // Stats

        foreach (Barrier barrier in barriers)
        {
            SeenBarrier? seenBarrier = seenBarriers.FirstOrDefault(sb => sb.Barrier == barrier);

            if (seenBarrier != null)
            {
                report.AddEntry(
                    ReportGroup.Stats,
                    new GenericReportEntry(
                        "Barrier `" + seenBarrier.Barrier.OsmValue + "` was seen " + seenBarrier.Count + " times."
                    )
                );
            }
            else
            {
                report.AddEntry(
                    ReportGroup.Stats,
                    new GenericReportEntry(
                        "Barrier `" + barrier.OsmValue + "` was not seen in data."
                    )
                );
            }
        }
        
        // TODO
        
        // todo: generic barrier yes
        
        // todo: planters are also tagged as blocks if on way
    }


    private record Barrier(string OsmValue, bool MustBeOnWay);
    
    
    private record SeenBarrier(Barrier Barrier, int Count)
    {
        public int Count { get; set; } = Count;
    }


    private record NonWayBarrierNode(Barrier Barrier, OsmNode Node, bool Bad);
        
        
    private enum ReportGroup
    {
        NonWayBarriers,
        Stats
    }
}