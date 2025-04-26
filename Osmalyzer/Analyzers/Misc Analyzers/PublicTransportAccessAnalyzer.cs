using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class PublicTransportAccessAnalyzer : Analyzer
{
    public override string Name => "Public Transport Access";

    public override string Description => "This report checks that ways with public transport routes have expected and valid access values.";

    public override AnalyzerGroup Group => AnalyzerGroups.PublicTransport;


    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(LatviaOsmAnalysisData) };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();
           
        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract osmRoutes = osmMasterData.Filter(
            new IsRelation(),
            new HasValue("type", "route"),
            new OrMatch(
                new HasAnyValue("route", "tram", "bus", "trolleybus"),
                new HasAnyValue("disused:route", "tram", "bus", "trolleybus")
            )            
        );
            
        // Prepare report groups

        report.AddGroup(ReportGroup.BlockingPsv, "Blocking PSV");

        report.AddGroup(ReportGroup.RedundantPsv, "Redundant PSV", @"It is pointless to specify default access values, when no other ""higher"" access group restricts it. If the intention is to mark ways somehow meant or designated for public transport, then the value should be `psv=designated`.");

        report.AddGroup(ReportGroup.PsvOverAccessAlreadyPsv, "PSV value redundunt access override");

        report.AddGroup(ReportGroup.UnexpectedAccess, "Access value invalid");
            
        report.AddGroup(ReportGroup.UnexpectedOneway, "Oneway value invalid");

        report.AddGroup(ReportGroup.OnewaypsvOnNonOneway, "Bad oneway PSV values on non-oneway");

        report.AddGroup(ReportGroup.BadPsvOnRestrictedAccess, "Bad PSV value on restricted");

        report.AddGroup(ReportGroup.BusShouldBePsv, "Bus instead of PSV", @"There are no laws or regulations in Latvia that apply specifically to buses but not other modes of public transport, notably taxis. All laws that mention public transport discuss it in context of public transportation and not specific vehicle types, like trolleybuses, minibuses, coaches, etc. So bus and oneway:bus is likely always wrong or at least imprecise on generic roads and should be psv and oneway:psv. There may be some rare individual cases where a small service section is specifically for buses, like at bus station service areas, but then a public transport route is unlikely to run there.");

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
                
            if (psv is null)
            {
                // By itself, it's normal to not need to specify psv
            }
            else if (psv is "no")
            {
                // psv=no

                report.AddEntry(
                    ReportGroup.BlockingPsv,
                    new IssueReportEntry(
                        "Way has `psv=no` -- " + osmRouteWay.OsmViewUrl,
                        osmRouteWay.GetAverageCoord(),
                        MapPointStyle.Problem
                    )
                );
            }
            else if (psv is "yes")
            {
                if (access is null)
                {
                    if (vehicle is null)
                    {
                        // psv=yes
                        report.AddEntry(
                            ReportGroup.RedundantPsv,
                            new IssueReportEntry(
                                "Way has no `access` (or `vehicle`) value, but redundant `psv=yes` -- " + osmRouteWay.OsmViewUrl,
                                osmRouteWay.GetAverageCoord(),
                                MapPointStyle.Problem
                            )
                        );
                    }
                }
                else if (access is "yes")
                {
                    // access=yes + psv=yes
                    report.AddEntry(
                        ReportGroup.RedundantPsv,
                        new IssueReportEntry(
                            "Way has `access=yes` value, but redundant `psv=yes` -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.GetAverageCoord(),
                            MapPointStyle.Problem
                        )
                    );
                }
                else if (vehicle is "yes")
                {
                    // access=yes + psv=yes
                    report.AddEntry(
                        ReportGroup.RedundantPsv,
                        new IssueReportEntry(
                            "Way has `vehicle=yes` value, but redundant `psv=yes` -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.GetAverageCoord(),
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
                if (psv is null)
                {
                    if (bus is null) // we would report bus should be psv then
                    {
                        // access=no
                        report.AddEntry(
                            ReportGroup.BadPsvOnRestrictedAccess,
                            new IssueReportEntry(
                                "Way has `access=" + access + "`, but no `psv` value -- " + osmRouteWay.OsmViewUrl,
                                osmRouteWay.GetAverageCoord(),
                                MapPointStyle.Problem
                            )
                        );
                    }
                }
                else if (psv is "yes")
                {
                    // todo: every other access tag is missing - should just be access=psv
                }
                else if (psv is not "yes" and not "designated")
                {
                    if (bus is null) // we would report bus should be psv then
                    {
                        // access=no + psv=hello
                        report.AddEntry(
                            ReportGroup.BadPsvOnRestrictedAccess,
                            new IssueReportEntry(
                                "Way has `access=no`, but unexpected `psv=" + psv + "` value -- " + osmRouteWay.OsmViewUrl,
                                osmRouteWay.GetAverageCoord(),
                                MapPointStyle.Problem
                            )
                        );
                    }
                }
            }
            else if (access is "psv") 
            {
                if (psv is not null)
                {
                    // access=psv + psv=hi
                    report.AddEntry(
                        ReportGroup.PsvOverAccessAlreadyPsv,
                        new IssueReportEntry(
                            "Way already has `access=psv`, but also specifies `psv=" + psv + "` -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.GetAverageCoord(),
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
                        osmRouteWay.GetAverageCoord(),
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
                if (oneway_psv is not null)
                {
                    // oneway=no + oneway:psv=yes
                    report.AddEntry(
                        ReportGroup.OnewaypsvOnNonOneway,
                        new IssueReportEntry(
                            "Way is `oneway=no`, but has `oneway:psv=" + oneway_psv + "` -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.GetAverageCoord(),
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
                        osmRouteWay.GetAverageCoord(),
                        MapPointStyle.Problem
                    )
                );
            }

            // What about bus?
                
            if (bus is not null)
            {
                if (bus is "no")
                {
                    // oneway:bus=no
                    report.AddEntry(
                        ReportGroup.BusShouldBePsv,
                        new IssueReportEntry(
                            "`bus=no` instead of `psv=no`" + 
                            (psv is not null 
                                ? psv is "no" 
                                    ? " (already set)" 
                                    : " (set, but value is `" + psv + "`)" 
                                : ""
                            ) + 
                            " on " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.GetAverageCoord(),
                            MapPointStyle.Problem
                        )
                    );
                }
                else
                {
                    // oneway:bus=hello
                    report.AddEntry(
                        ReportGroup.BusShouldBePsv,
                        new IssueReportEntry(
                            "Unexpected `bus=" + bus + "` value, instead of `psv` if applicable -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.GetAverageCoord(),
                            MapPointStyle.Problem
                        )
                    );
                }
            }

            if (oneway_bus is not null)
            {
                if (oneway_bus is "no")
                {
                    // oneway:bus=no
                    report.AddEntry(
                        ReportGroup.BusShouldBePsv,
                        new IssueReportEntry(
                            "`oneway:bus=no` instead of `oneway:psv=no`" + 
                            (oneway_psv is not null 
                                ? oneway_psv is "no" 
                                    ? " (already set)" 
                                    : " (set, but value is `" + oneway_psv + "`)" 
                                : ""
                            ) + 
                            " on " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.GetAverageCoord(),
                            MapPointStyle.Problem
                        )
                    );
                }
                else
                {
                    // oneway:bus=hello
                    report.AddEntry(
                        ReportGroup.BusShouldBePsv,
                        new IssueReportEntry(
                            "Unexpected `oneway:bus=" + oneway_bus + "` value, should be `oneway:psv` if applicable -- " + osmRouteWay.OsmViewUrl,
                            osmRouteWay.GetAverageCoord(),
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
        BusShouldBePsv,
        OnewaypsvOnNonOneway,
        UnexpectedOneway,
        UnexpectedAccess,
        BadPsvOnRestrictedAccess,
        RedundantPsv,
        BlockingPsv,
        PsvOverAccessAlreadyPsv
    }
}