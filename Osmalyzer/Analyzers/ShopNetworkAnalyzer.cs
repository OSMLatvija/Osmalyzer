using System;
using System.Collections.Generic;
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
            typeof(LatsShopsAnalysisData), 
            typeof(ElviShopsAnalysisData), 
            //typeof(TopShopsAnalysisData)
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
            
            List<ShopParser> parsers = new List<ShopParser>()
            {
                new ElviShopParser(),
                new LatsShopParser(),
                new TopShopParser()
            };

            foreach (ShopParser parser in parsers.Where(p => p.Enabled))
            {
                report.AddGroup(parser.Name, parser.Name + " shops:");

                ShopListAnalysisData shopData = (ShopListAnalysisData)datas.First(ad => ad.GetType() == parser.DataType);

                string source = File.ReadAllText(shopData.DataFileName);

                List<ShopData> listedShops = parser.GetShops(source);
                
                // Parse

                OsmDataExtract brandShops = osmShops.Filter(
                    new OrMatch(
                        new HasValue("name", parser.OsmName, false),
                        new HasValue("operator", parser.OsmName, false),
                        new HasValue("brand", parser.OsmName, false)
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
                                parser.Name,
                                new IssueReportEntry(
                                    "Shop matched for " + ListedShopString(listedShop) +
                                    " as " + OsmShopString(exactMatchedShop) +
                                    " , but it's far away - " + distance.Value.ToString("F0") + " m.",
                                    listedShop.Coord
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
                                parser.Name,
                                new IssueReportEntry(
                                    "No expected shop for " + ListedShopString(listedShop) +
                                    " , closest " + OsmShopString(closestMatchedShop) + 
                                    " at " + distance!.Value.ToString("F0") + " m.",
                                    listedShop.Coord
                                )
                            );
                        }
                        else
                        {
                            report.AddEntry(
                                parser.Name,
                                new IssueReportEntry(
                                    "No expected shop for " + ListedShopString(listedShop) + " , and no shops nearby.",
                                    listedShop.Coord
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
                            parser.Name,
                            new IssueReportEntry(
                                "OSM shop " + OsmShopString(osmShop) +
                                " matched " + multimatch.Count() + " times to listed shops - " +
                                string.Join(", ", multimatch.Select(m => ListedShopString(m.listedShop) + " (" + m.distance.ToString("F0") + " m) "))
                            )
                        );
                    }
                }

                report.AddEntry(
                    parser.Name,
                    new DescriptionReportEntry(
                        "Matching " + listedShops.Count + " shops from the " + parser.Name + " website shop list ( " + shopData.ShopListUrl + " ) to OSM elements. " +
                        "Matched " + matchedOsmShops.Count + " shops."
                    )
                );
            }


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


        private abstract class ShopParser
        {
            public abstract bool Enabled { get; }
            
            public abstract Type DataType { get; }

            public abstract string Name { get; }
            
            public abstract string OsmName { get; }

            public abstract List<ShopData> GetShops(string source);
        }

        private abstract class ShopParserWithData<T> : ShopParser
            where T : ShopListAnalysisData
        {
            public override Type DataType => typeof(T);
        }

        private class LatsShopParser : ShopParserWithData<LatsShopsAnalysisData>
        {
            public override bool Enabled => true;
            
            public override string Name => "LaTS";
            
            public override string OsmName => Name;

            public override List<ShopData> GetShops(string source)
            {
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
                    
                    address = Regex.Replace(address, @", LV-\d{4}$", "");
                    
                    listedShops.Add(new ShopData(address, new OsmCoord(lat, lon)));
                }

                return listedShops;
            }
        }

        private class ElviShopParser : ShopParserWithData<ElviShopsAnalysisData>
        {
            public override bool Enabled => true;

            public override string Name => "Elvi";
            
            public override string OsmName => Name;

            public override List<ShopData> GetShops(string source)
            {
                // value: "Kursīši, Saldus nov., Bērzu iela 1-18, LV-3890, ELVI veikals",
                // data: [
                //         {
                //             id: 1643,
                //             link: "https://elvi.lv/veikali/kursisi-elvi-veikals/",
                //             lat: 56.512548,
                //             lng: 22.405646                                    }
                //   ]
                // },       
                
                MatchCollection matches = Regex.Matches(
                    source, 
                    @"value: ""([^""]+)"",\s*data: \[\s*\{\s*id:\s\d+,\s*link:\s\""[^""]+\"",\s*lat: (\d{2}.\d{1,15}),\s*lng:(\s\d{2}.\d{1,15})\s*"
                );
                
                List<ShopData> listedShops = new List<ShopData>();
                
                foreach (Match match in matches)
                {
                    double lat = double.Parse(match.Groups[2].ToString());
                    double lon = double.Parse(match.Groups[3].ToString());
                    string address = HtmlEntity.DeEntitize(match.Groups[1].ToString()).Trim();
                    
                    address = Regex.Replace(address, @", ELVI veikals$", "");
                    address = Regex.Replace(address, @", LV-\d{4}$", "");

                    listedShops.Add(new ShopData(address, new OsmCoord(lat, lon)));
                }

                return listedShops;
            }
        }

        private class TopShopParser : ShopParserWithData<TopShopsAnalysisData>
        {
            public override bool Enabled => false;

            public override string Name => "Top!";
            
            public override string OsmName => "Top";

            public override List<ShopData> GetShops(string source)
            {
                // It's not in source, it's using google map with embedded data that I would need to somehow get
                
                throw new NotImplementedException();
            }
        }


        private class ShopData
        {
            public string Address { get; }
            public OsmCoord Coord { get; }

            
            public ShopData(string address, OsmCoord coord)
            {
                Address = address;
                Coord = coord;
            }
        }
    }
}