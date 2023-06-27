using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class ShopNetworkAnalyzer : Analyzer
    {
        public override string Name => "Shop Networks";

        public override string? Description => null;


        public override List<Type> GetRequiredDataTypes() => new List<Type>()
        {
            typeof(OsmAnalysisData), 
            typeof(LatsShopsAnalysisData)
        };
        

        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Start report

            report.AddGroup(ReportGroup.Lats, "LaTS shops:");
            
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

            OsmMasterData osmMasterData = osmData.MasterData;
                
            OsmDataExtract osmShops = osmMasterData.Filter(
                new HasKey("shop")
            );

            OsmDataExtract latsShops = osmShops.Filter(
                new OrMatch(
                    new HasValue("name", "LaTS", false),
                    new HasValue("operator", "LaTS", false),
                    new HasValue("brand", "LaTS", false)
                )
            );

            // Load Shop data
            
            LatsShopsAnalysisData latsData = datas.OfType<LatsShopsAnalysisData>().First();

            string source = File.ReadAllText(latsData.DataFileName);

            //this is the pretty list, but doesn't have most MatchCollection matches = Regex.Matches(source, @"<h4>([^<]+)</h4>\s*</div>\s*</div>\s*<div[^>]*>\s*<div[^>]*>\s*(?:<img[^>]*>\s*)?<a[^>]*? data-lat=""(\d{2}\.\d{1,7})""\s*data-long=""(\d{2}\.\d{1,7})""");

            // markers.push({
            //     coordinates: {lat: 56.9069266, lng: 24.1982285},
            //     image: '/img/map-marker.png',
            //     title: 'Veikals',
            //     info: '<div id="contentMap">' +
            //         '<h3>Veikals:</h3>' +
            //         '<div id="bodyContentMap">' +
            //                                         '<p>Plostu iela 29, Rīga, LV-1057</p>' +
            //                                         '<p>Tel.nr.: 67251697</p>' +
            //                                         '<p>Šodien atvērts 9:00-19:00</p>' +
            //                     '</div></div>'
            // });          
            
            MatchCollection matches = Regex.Matches(
                source, 
                @"markers\.push\({\s*coordinates: {lat: (\d{2}.\d{1,8}), lng: (\d{2}.\d{1,8})},[^}]*?<p>([^<]+)</p>"
            );
            
            List<ShopData> listedShops = new List<ShopData>();
            
            foreach (Match match in matches)
            {
                double lat = double.Parse(match.Groups[1].ToString());
                double lon = double.Parse(match.Groups[2].ToString());
                string address = HtmlEntity.DeEntitize(match.Groups[3].ToString()).Trim();
                
                listedShops.Add(new ShopData(address, lat, lon));
            }
            
            // Parse

            List<(OsmElement, ShopData, double)> matchedOsmShops = new List<(OsmElement, ShopData, double)>();
            
            foreach (ShopData listedShop in listedShops)
            {
                OsmElement? exactMatchedShop = latsShops.GetClosestElementTo(listedShop.Lat, listedShop.Lon, 500, out double? distance);

                if (exactMatchedShop != null)
                {
                    matchedOsmShops.Add((exactMatchedShop, listedShop, distance!.Value));
                    
                    if (distance > 50)
                    {
                        report.AddEntry(
                            ReportGroup.Lats,
                            new Report.IssueReportEntry(
                                "Shop matched for " + ListedShopString(listedShop) + 
                                " as " + OsmShopString(exactMatchedShop) +
                                " , but it's far away - " + distance.Value.ToString("F0") + " m."
                            )
                        );
                    }
                }
                else
                {
                    OsmElement? closestMatchedShop = osmShops.GetClosestElementTo(listedShop.Lat, listedShop.Lon, 200);

                    if (closestMatchedShop != null)
                    {

                        report.AddEntry(
                            ReportGroup.Lats,
                            new Report.IssueReportEntry(
                                "No expected shop for " + ListedShopString(listedShop) +
                                " , closest " + OsmShopString(closestMatchedShop)
                            )
                        );
                    }
                    else
                    {
                        report.AddEntry(
                                ReportGroup.Lats,
                                new Report.IssueReportEntry(
                                    "No expected shop for " + ListedShopString(listedShop) + " , and no shops nearby."
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
                        ReportGroup.Lats,
                        new Report.IssueReportEntry(
                            "OSM shop " + OsmShopString(osmShop) +
                            " matched " + multimatch.Count() + " times to listed shops - " +
                            string.Join(", ", multimatch.Select(m => ListedShopString(m.listedShop) + " (" + m.distance.ToString("F0") + " m) "))
                        )
                    );
                }
            }
            
            report.AddEntry(
                ReportGroup.Lats,
                new Report.DescriptionReportEntry(
                    "Matching " + listedShops.Count + " shops from the LaTS website shop list ( " + latsData.ShopListUrl + " ) to OSM elements. " +
                    "Matched " + matchedOsmShops.Count + " shops."
                )
            );

            
            // todo: match backwards
            

            static string ListedShopString(ShopData listedShop)
            {
                return
                    "\"" + TrimPostcode(listedShop.Address) + "\" " +
                    "found around https://www.openstreetmap.org/#map=19/" + listedShop.Lat.ToString("F5") + "/" + listedShop.Lon.ToString("F5");
                

                static string TrimPostcode(string address)
                {
                    return Regex.Replace(address, @", LV-\d{4}$", "");
                }
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


        private class ShopData
        {
            public string Address { get; }
            public double Lat { get; }
            public double Lon { get; }

            
            public ShopData(string address, double lat, double lon)
            {
                Address = address;
                Lat = lat;
                Lon = lon;
            }
        }
        
        
        private enum ReportGroup
        {
            Lats
        }
    }
}