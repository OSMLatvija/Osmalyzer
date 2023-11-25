using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public abstract class ShopNetworkAnalyzer<T> : Analyzer where T : ShopListAnalysisData
{
    public override string Name => ShopName + " Shop Networks";

    public override string Description => "This report checks that all " + ShopName + " shops listed on brand's website are found on the map. " +
                                          "This supposes that brand shops are tagged correctly to match among multiple.";


    protected abstract string ShopName { get; }

    protected abstract List<string> ShopOsmNames { get; }



    public override List<Type> GetRequiredDataTypes() => new List<Type>()
    {
        typeof(OsmAnalysisData), 
        typeof(T) // shop list data
    };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
                
        OsmDataExtract osmShops = osmMasterData.Filter(
            new HasKey("shop")
        );

        // Load Shop data

        ShopListAnalysisData shopData = datas.OfType<ShopListAnalysisData>().First();

        List<ShopData> listedShops = shopData.GetShops();

        // Parse

        report.AddGroup(ShopName, ShopName + " shops:");

        OsmDataExtract brandShops = osmShops.Filter(
            new OrMatch(
                new HasAnyValue("name", ShopOsmNames, false),
                new HasAnyValue("operator", ShopOsmNames, false),
                new HasAnyValue("brand", ShopOsmNames, false)
            )
        );

        List<(OsmElement, ShopData, double)> matchedOsmShops = new List<(OsmElement, ShopData, double)>();

        foreach (ShopData listedShop in listedShops)
        {
            OsmElement? exactMatchedShop = brandShops.GetClosestElementTo(listedShop.Coord, 500, out double? distance);

            if (exactMatchedShop != null)
            {
                matchedOsmShops.Add((exactMatchedShop, listedShop, distance!.Value));

                if (distance > 50)
                {
                    report.AddEntry(
                        ShopName,
                        new IssueReportEntry(
                            "Shop matched for " + ListedShopString(listedShop) +
                            " as " + OsmShopString(exactMatchedShop) +
                            " , but it's far away - " + distance.Value.ToString("F0") + " m.",
                            listedShop.Coord,
                            MapPointStyle.Dubious
                        )
                    );
                }
            }
            else
            {
                OsmElement? closestMatchedShop = osmShops.GetClosestElementTo(listedShop.Coord, 200, out distance);

                if (closestMatchedShop != null)
                {
                    report.AddEntry(
                        ShopName,
                        new IssueReportEntry(
                            "No expected shop for " + ListedShopString(listedShop) +
                            " , closest " + OsmShopString(closestMatchedShop) +
                            " at " + distance!.Value.ToString("F0") + " m.",
                            listedShop.Coord,
                            MapPointStyle.Problem
                        )
                    );
                }
                else
                {
                    report.AddEntry(
                        ShopName,
                        new IssueReportEntry(
                            "No expected shop for " + ListedShopString(listedShop) + " , and no shops nearby.",
                            listedShop.Coord,
                            MapPointStyle.Problem
                        )
                    );
                }
            }
        }

        IEnumerable<IGrouping<OsmElement, (OsmElement, ShopData, double)>> sameOsmShopMatches = matchedOsmShops.GroupBy(ms => ms.Item1);

        foreach (IGrouping<OsmElement, (OsmElement osmShop, ShopData listedShop, double distance)> multimatch in sameOsmShopMatches)
        {
            if (multimatch.Count() > 1)
            {
                OsmElement osmShop = multimatch.First().osmShop; // any will do, they are all the same

                report.AddEntry(
                    ShopName,
                    new IssueReportEntry(
                        "OSM shop " + OsmShopString(osmShop) +
                        " matched " + multimatch.Count() + " times to listed shops - " +
                        string.Join(", ", multimatch.Select(m => ListedShopString(m.listedShop) + " (" + m.distance.ToString("F0") + " m) "))
                    )
                );
            }
        }

        report.AddEntry(
            ShopName,
            new DescriptionReportEntry(
                "Matching " + listedShops.Count + " shops from the " + ShopName + " website shop list ( " + shopData.ShopListUrl + " ) to OSM elements. " +
                "Matched " + matchedOsmShops.Count + " shops."
            )
        );


        // todo: match backwards
            

        static string ListedShopString(ShopData listedShop)
        {
            return
                "\"" + listedShop.Address + "\" " +
                "found around " + listedShop.Coord.OsmUrl;
        }

        static string OsmShopString(OsmElement osmShop)
        {
            return 
                (osmShop.HasKey("name") ? 
                    "\"" + osmShop.GetValue("name") + "\"" : 
                    "unnamed"
                ) + " " + 
                osmShop.OsmViewUrl;
                    
            // todo: brand operator whatever else we used to match
        }
    }
}