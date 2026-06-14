namespace Osmalyzer;

[UsedImplicitly]
public class PublicTransportAccessAnalyzer : Analyzer
{
    public override string Name => "Public Transport Access";

    public override string Description => "This report checks that ways with public transport routes have expected and valid access values.";

    public override AnalyzerGroup Group => AnalyzerGroup.PublicTransport;


    public override List<Type> GetRequiredDataTypes() => [ typeof(LatviaOsmAnalysisData) ];
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmData osmMasterData = osmData.MasterData;

        OsmData osmRoutes = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("type", "route"),
            new OrMatch(
                new HasAnyValue("route", "tram", "bus", "trolleybus"),
                new HasAnyValue("disused:route", "tram", "bus", "trolleybus")
            )            
        );
            
        // Prepare report groups

        report.AddGroup(ReportGroup.BlockingBus, "Blocking bus=*");

        report.AddGroup(ReportGroup.RedundantBus, "Redundant bus=*", @"It is pointless to specify default access values, when no other ""higher"" access group restricts it. If the intention is to mark ways somehow meant or designated for public transport, then the value should be `bus=designated`.");

        report.AddGroup(ReportGroup.BusOverAccessAlreadyBus, "bus=* value redundant access override");

        report.AddGroup(ReportGroup.UnexpectedAccess, "Access value invalid");
            
        report.AddGroup(ReportGroup.UnexpectedOneway, "Oneway value invalid");

        report.AddGroup(ReportGroup.OnewayBusOnNonOneway, "Bad oneway bus=* values on non-oneway");

        report.AddGroup(ReportGroup.BadBusOnRestrictedAccess, "Bad bus=* value on restricted");

        report.AddGroup(ReportGroup.PsvShouldBeBus, "psv=* instead of bus=*", @"While technically psv=* is a valid higher access group for bus=*, the laws or regulations in Latvia that apply to public transport actually encompass only buses (like trolleybuses, minibuses, coaches, etc.). So psv and oneway:psv is likely always wrong or at least pointlessly generic and should be bus and oneway:bus.");

        // Parse

        Dictionary<long, OsmWay> osmRouteWays = new Dictionary<long, OsmWay>();

        foreach (OsmRelation osmRoute in osmRoutes.Relations)
            foreach (OsmWay osmRouteWay in osmRoute.GetElementsWithRole<OsmWay>(""))
                osmRouteWays.TryAdd(osmRouteWay.Id, osmRouteWay);

        foreach ((long _, OsmWay osmRouteWay) in osmRouteWays)
        {
            string? access = osmRouteWay.GetValue("access");
            string? vehicle = osmRouteWay.GetValue("vehicle");
            string? psv = osmRouteWay.GetValue("psv");
            string? bus = osmRouteWay.GetValue("bus");
            string? oneway = osmRouteWay.GetValue("oneway");
            string? oneway_psv = osmRouteWay.GetValue("oneway:psv");
            string? oneway_bus = osmRouteWay.GetValue("oneway:bus");
                
            // todo: collect all combinations and report in stat section
            // todo: see if I missed any cases
                
            // todo
            //string? tram = osmRouteWay.GetValue("tram");
            //string? trolleybus = osmRouteWay.GetValue("trolleybus");

            // What about psv by itself?
                
            if (bus is null)
            {
                // By itself, it's normal to not need to specify bus
            }
            else if (bus is "no")
            {
                // bus=no

                report.AddEntry(
                    ReportGroup.BlockingBus,
                    new IssueReportEntry(
                        "Way has `bus=no` -- " + osmRouteWay.OsmViewUrl,
                        osmRouteWay.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
            }
            else if (bus is "yes")
            {
                if (access is null)
                {
                    if (vehicle is null)
                    {
                        // bus=yes
                        report.AddEntry(
                            ReportGroup.RedundantBus,
                            new IssueReportEntry(
                                "Way has no `access` (or `vehicle`) value, but redundant `bus=yes` -- " + osmRouteWay.OsmViewUrl,
                                osmRouteWay.AverageCoord,
                                MapPointStyle.Problem
                            )
                        );
                    }
                }
                else if (access is "yes")
                {
                    // access=yes + bus=yes
                    report.AddEntry(
                        ReportGroup.RedundantBus,
                        new IssueReportEntry(
                            "Way has `access=yes` value, but redundant `bus=yes` -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
                else if (vehicle is "yes")
                {
                    // vehicle=yes + bus=yes
                    report.AddEntry(
                        ReportGroup.RedundantBus,
                        new IssueReportEntry(
                            "Way has `vehicle=yes` value, but redundant `bus=yes` -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }

            // What if access is set?

            if (access is null or "yes")
            {
                // Likely pointless, but not an error
            }
            else if (access is "no" or "private" or "destination")
            {
                if (bus is null)
                {
                    if (psv is null) // we would report psv should be bus then
                    {
                        // access=no
                        report.AddEntry(
                            ReportGroup.BadBusOnRestrictedAccess,
                            new IssueReportEntry(
                                "Way has `access=" + access + "`, but no `bus` value -- " + osmRouteWay.OsmViewUrl,
                                osmRouteWay.AverageCoord,
                                MapPointStyle.Problem
                            )
                        );
                    }
                }
                else if (bus is "yes")
                {
                    // todo: every other access tag is missing - should just be access=psv
                }
                else if (bus is not "yes" and not "designated")
                {
                    if (psv is null) // we would report psv should be bus then
                    {
                        // access=no + bus=hello
                        report.AddEntry(
                            ReportGroup.BadBusOnRestrictedAccess,
                            new IssueReportEntry(
                                "Way has `access=no`, but unexpected `bus=" + bus + "` value -- " + osmRouteWay.OsmViewUrl,
                                osmRouteWay.AverageCoord,
                                MapPointStyle.Problem
                            )
                        );
                    }
                }
            }
            else if (access is "bus") 
            {
                if (bus is not null)
                {
                    // access=bus + bus=hi
                    report.AddEntry(
                        ReportGroup.BusOverAccessAlreadyBus,
                        new IssueReportEntry(
                            "Way already has `access=bus`, but also specifies `bus=" + bus + "` -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }
            else // access other value
            {
                // access=hello
                report.AddEntry(
                    ReportGroup.UnexpectedAccess,
                    new IssueReportEntry(
                        "Unexpected `access=" + access + "` value -- " + osmRouteWay.OsmViewUrl,
                        osmRouteWay.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
            }
                
            // What if oneway is set?

            if (oneway is "yes")
            {
                // By itself, not an issue
            }
            else if (oneway is "no")
            {
                if (oneway_bus is not null)
                {
                    // oneway=no + oneway:bus=yes
                    report.AddEntry(
                        ReportGroup.OnewayBusOnNonOneway,
                        new IssueReportEntry(
                            "Way is `oneway=no`, but has `oneway:bus=" + oneway_bus + "` -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }
            else if (oneway is not null) // oneway other value
            {
                // oneway=maybe
                report.AddEntry(
                    ReportGroup.UnexpectedOneway,
                    new IssueReportEntry(
                        "Unexpected `oneway=" + oneway + "` value -- " + osmRouteWay.OsmViewUrl,
                        osmRouteWay.AverageCoord,
                        MapPointStyle.Problem
                    )
                );
            }

            // What about psv?
                
            if (psv is not null)
            {
                if (psv is "no")
                {
                    // oneway:bus=no
                    report.AddEntry(
                        ReportGroup.PsvShouldBeBus,
                        new IssueReportEntry(
                            "`psv=no` instead of `bus=no`" + 
                            (bus is not null 
                                ? bus is "no" 
                                    ? " (already set)" 
                                    : " (set, but value is `" + bus + "`)" 
                                : ""
                            ) + 
                            " on " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
                else
                {
                    // oneway:bus=hello
                    report.AddEntry(
                        ReportGroup.PsvShouldBeBus,
                        new IssueReportEntry(
                            "Unexpected `psv=" + psv + "` value, instead of `bus` if applicable -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }

            if (oneway_psv is not null)
            {
                if (oneway_psv is "no")
                {
                    // oneway:psv=no
                    report.AddEntry(
                        ReportGroup.PsvShouldBeBus,
                        new IssueReportEntry(
                            "`oneway:psv=no` instead of `oneway:bus=no`" + 
                            (oneway_bus is not null 
                                ? oneway_bus is "no" 
                                    ? " (already set)" 
                                    : " (set, but value is `" + oneway_bus + "`)" 
                                : ""
                            ) + 
                            " on " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
                else
                {
                    // oneway:psv=hello
                    report.AddEntry(
                        ReportGroup.PsvShouldBeBus,
                        new IssueReportEntry(
                            "Unexpected `oneway:psv=" + oneway_psv + "` value, should be `oneway:bus` if applicable -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.AverageCoord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }
        }
            
            
        // todo: check route travel direction and see if oneway is violated
    }
        

    private enum ReportGroup
    {
        PsvShouldBeBus,
        OnewayBusOnNonOneway,
        UnexpectedOneway,
        UnexpectedAccess,
        BadBusOnRestrictedAccess,
        RedundantBus,
        BlockingBus,
        BusOverAccessAlreadyBus
    }
}