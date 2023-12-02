using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class TopShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Top! Shops";

    public override string ReportWebLink => @"https://www.toppartika.lv/veikali/";


    protected override string DataFileIdentifier => "shops-top";


    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        for (int i = 1; i <= 5; i++)
        {
            string fileName = Path.Combine(CacheBasePath, DataFileIdentifier +@"-" + i + @".json");

            WebsiteDownloadHelper.DownloadPost(
                "https://www.toppartika.lv/d/",
                new[]
                {
                    ("action", "getShops"), // could also be action=getShop&id=185, which returns html
                    ("reg", i.ToString()), // this is "reģions", 0 for all, 1-5 for Kurzeme Zemgale Rīgas reģions Vidzeme Latgale
                    ("nov", "0"), // this is "novads", breaks down regions
                    ("s", "") // no idea what this is
                },
                fileName
            );
            
            // Note that region=0 return a list without any data, which is why I'm not using that:
            // {
            // 	"top": "",
            // 	"shops": [
            // 		{
            // 			"gps": "57.166163, 26.769701",
            // 			"id": "31"
            // 		},
            // 		{
            // 			"gps": "57.773871, 26.019552",
            // 			"id": "32"
            // 		},
            //      ETC.
            // 	],
            // 	"left": "",
            // 	"cnt": 207
            // }
        }
    }

    public override void Prepare()
    {
        _shops = new List<ShopData>();

        for (int i = 1; i <= 5; i++)
        {
            string fileName = Path.Combine(CacheBasePath, DataFileIdentifier + @"-" + i + @".json");
        
            string source = File.ReadAllText(fileName);

            // Normally:
            // {
            // 	"top": "HTML CONTENT",
            // 	"shops": [
            // 		{
            // 			"gps": "56.7190047, 21.6073235",
            // 			"id": "86"
            // 		},
            // 		{
            // 			"gps": "56.5051124, 21.0110997",
            // 			"id": "87"
            // 		},
            //      ETC.
            // 	],
            // 	"left": "<h4>Aizputes nov.:</h4><a href=\"#\" data-gps=\"56.7190047, 21.6073235\" data-id=\"86\">Zvaigžņu iela 2, Aizpute, Dienvidkurzemes nov., LV-3456 <span>❯</span></a><a href=\"#\" data-gps=\"56.72416029999999, 21.5986474\" data-id=\"92\">Saules iela 3B, Aizpute, Dienvidkurzemes nov., LV-3456 <span>❯</span></a><a href=\"#\" data-gps=\"56.732375, 21.390684\" data-id=\"93\">\"Ezeriņi\", Cīrava, Cīravas pag., Dienvidkurzemes nov., LV-3453 <span>❯</span></a><a href=\"#\" data-gps=\"56.7215305, 21.6039028\" data-id=\"99\">Pasta iela 7, Aizpute, Dienvidkurzemes nov., LV-3456 <span>❯</span></a><a href=\"#\" data-gps=\"56.729474, 21.733235\" data-id=\"105\">'Veikals gatve'', Kazdanga, Kazdangas pag., Dienvidkurzemes nov., LV-3457 <span>❯</span></a><a href=\"#\" data-gps=\"57.078675, 24.328741\" data-id=\"323\">Gaujas iela 20, Ādaži, Ādažu pag., Ādažu nov., LV-2164 <span>❯</span></a><h4>Brocēnu nov.:</h4><a href=\"#\" data-gps=\"56.6804187, 22.5685818\" data-id=\"122\">Lielcieceres iela 11, Brocēni, Saldus nov., LV-3851 <span>❯</span></a><h4>Grobiņas nov\n\t\t.:</h4><a href=\"#\" data-gps=\"56.5351129, 21.1681701\" data-id=\"89\">Lielā iela 74, Grobiņa, Dienvidkurzemes nov., LV-3430 <span>❯</span></a><a href=\"#\" data-gps=\"56.5321365, 21.1569656\" data-id=\"100\">M. Namiķa iela 1, Grobiņa, Dienvidkurzemes nov., LV-3430 <span>❯</span></a><a href=\"#\" data-gps=\"56.591746, 21.138296\" data-id=\"101\">\"Smilgas\", Kapsēde, Medzes pag., Dienvidkurzemes nov., LV-3461 <span>❯</span></a><a href=\"#\" data-gps=\"56.47730, 21.20044\" data-id=\"110\">Centra iela 6 - 2/3, Dubeņi, Grobiņas pag., Dienvidkurzemes nov., LV-3438 <span>❯</span></a><h4>Nīcas nov.:</h4><a href=\"#\" data-gps=\"56.348363, 21.065977\" data-id=\"102\">\"Gads\", Nīca, Nīcas pag., Dienvidkurzemes nov., LV-3473 <span>❯</span></a><h4>Pāvilostas nov.:</h4><a href=\"#\" data-gps=\"56.697554, 21.196286\" data-id=\"96\">\"Veikalnieki\", Vērgale, Vērgales pag., Dienvidkurzemes nov., LV-3463 <span>❯</span></a><a href=\"#\" data-gps=\"56.888846, 21.1792688\" data-id=\"97\">Tirgus iela 1, Pāvilosta, Dienvidkurzemes nov., LV-3466 <span>❯</span></a><a href=\"#\" data-gps=\"56.887209, 21.189195\" data-id=\"300\">Dzintaru iela 69, Pāvilosta, Dienvidkurzemes nov., LV-3466 <span>❯</span></a><h4>Priekules nov\n\t\t.:</h4><a href=\"#\" data-gps=\"56.447360, 21.585173\" data-id=\"94\">Galvenā iela 1, Priekule, Dienvidkurzemes nov., LV-3434 <span>❯</span></a><h4>Saldus nov.:</h4><a href=\"#\" data-gps=\"56.666327, 22.499880\" data-id=\"111\">Rīgas iela 22 – NT, Saldus, Saldus nov. <span>❯</span></a><h4>Vaiņodes nov.:</h4><a href=\"#\" data-gps=\"56.4168288, 21.8546207\" data-id=\"95\">Brīvības iela 15, Vaiņode, Vaiņodes pag., Dienvidkurzemes nov., LV-3435 <span>❯</span></a><h4>Liepāja:</h4><a href=\"#\" data-gps=\"56.5051124, 21.0110997\" data-id=\"87\">Kuršu laukums 11, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.509017, 21.000968\" data-id=\"88\">Vītolu iela 30, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.4896091, 21.0037568\" data-id=\"90\">Klaipēdas iela 80, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.546365, 21.068585\" data-id=\"91\">Skrundas iela 20b, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.5046678, 21.010806\" data-id=\"103\">Peldu iela 44 NT 7, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.508554, 21.0171537\" data-id=\"108\">Baznīcas iela 20, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.5109003, 21.0065795\" data-id=\"112\">Jūras iela 25/29, Liepāja  <span>❯</span></a><a href=\"#\" data-gps=\"56.5235151, 21.0188928\" data-id=\"113\">Dzelzceļnieku iela 16, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.51915709999999, 21.0150017\" data-id=\"114\">Jelgavas iela 37, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.5550853, 21.008249\" data-id=\"117\">Atmodas bulvāris 8F, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.55325999999999, 21.0164495\" data-id=\"119\">P.Brieža 14, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.5482686, 21.0847046\" data-id=\"120\">Kārklu iela 1, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.5046678, 21.010806\" data-id=\"121\">O.Kalpaka iela 66-NT30, Liepāja <span>❯</span></a><a href=\"#\" data-gps=\"56.553999, 21.042504\" data-id=\"333\">Sila iela 3, Liepāja, LV-3402 <span>❯</span></a>",
            // 	"cnt": 32
            // }            
            
            // But direct download also adds at the top:
            // <br />
            // <b>Notice</b>:  Undefined index: front in <b>/home/www/front/_data.php</b> on line <b>6</b><br />        

            if (!source.StartsWith("{"))
                source = source[source.IndexOf('{')..];
            
            dynamic content = JsonConvert.DeserializeObject(source)!;

            string shopContent = content.left;
            
            // <h4>Aizputes nov.:</h4><a href="#" data-gps="56.7190047, 21.6073235" data-id="86">Zvaigžņu iela 2, Aizpute, Dienvidkurzemes nov., LV-3456 <span>❯</span></a><a href="#" data-gps="56.72416029999999, 21.5986474" data-id="92">Saules iela 3B, Aizpute, Dienvidkurzemes nov., LV-3456 <span>❯</span></a><a href="#" data-gps="56.732375, 21.390684" data-id="93">"Ezeriņi", Cīrava, Cīravas pag., Dienvidkurzemes nov., LV-3453 <span>❯</span></a><a href="#" data-gps="56.7215305, 21.6039028" data-id="99">Pasta iela 7, Aizpute, Dienvidkurzemes nov., LV-3456 <span>❯</span></a><a href="#" data-gps="56.729474, 21.733235" data-id="105">'Veikals gatve'', Kazdanga, Kazdangas pag., Dienvidkurzemes nov., LV-3457 <span>❯</span></a><a href="#" data-gps="57.078675, 24.328741" data-id="323">Gaujas iela 20, Ādaži, Ādažu pag., Ādažu nov., LV-2164 <span>❯</span></a><h4>Brocēnu nov.:</h4><a href="#" data-gps="56.6804187, 22.5685818" data-id="122">Lielcieceres iela 11, Brocēni, Saldus nov., LV-3851 <span>❯</span></a><h4>Grobiņas nov

            MatchCollection matches = Regex.Matches(shopContent, @"<a href=""#"" data-gps=""([^,]+), ([^""]+)"" data-id=""[^""]+"">([^<]+)<");
            // <a href="#" data-gps="56.7190047, 21.6073235" data-id="86">Zvaigžņu iela 2, Aizpute, Dienvidkurzemes nov., LV-3456 <
            
            if (matches.Count == 0)
                throw new Exception("Did not match any items on webpage");
            
            foreach (Match match in matches)
            {
                // I can get each shop data via same "API", but it would be one per call

                string address = Regex.Unescape(match.Groups[3].ToString()).Trim();
                // \"Ezeriņi\", Cīrava, Cīravas pag., Dienvidkurzemes nov., LV-3453 
                
                OsmCoord coord = new OsmCoord(
                    double.Parse(match.Groups[1].ToString()),
                    double.Parse(match.Groups[2].ToString())
                );

                _shops.Add(
                    new ShopData(
                        "Top!",
                        address,
                        coord
                    )
                );
            }
        }
    }
}