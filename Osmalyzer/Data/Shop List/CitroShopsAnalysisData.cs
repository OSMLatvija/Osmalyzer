using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class CitroShopsAnalysisData : ShopListAnalysisData
{
    public override string Name => "Citro Shops";

    public override string ReportWebLink => @"https://citro.lv/musu-veikali/";


    protected override string DataFileIdentifier => "shops-citro";


    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".html");

    public override IEnumerable<ShopData> Shops => _shops;

    
    private List<ShopData> _shops = null!; // only null until prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://citro.lv/musu-veikali/", 
            DataFileName
        );
    }

    public override void Prepare()
    {
        // APP.shops.list = [{"id":159426,"url":"https:\/\/www.citro.lv\/veikali\/citro-galerija-centrs","full_name":"Citro Galerija centrs","business_name":"citro galerija centrs","address_line_1":"audeju iela 16","keywords":null,"locality":"riga","icon":"https:\/\/citrobaltic-web-res.cloudinary.com\/image\/upload\/c_fit,f_auto,h_48,q_auto,w_48\/v1\/web-cms\/fd86ab20bf713e2cd42f2fbbe6a01a9459c20b4b.png","longitude":"24.11271384","latitude":"56.94801025","most_visited":1,"display":"\u201eCitro Galerija centrs\u201c, Aud\u0113ju iela 16, R\u012bga"},   
                
        string source = File.ReadAllText(DataFileName);

        MatchCollection matches = Regex.Matches(
            source, 
            @"\{\s*coords:\{([^\}]+)\},\s*shopLogo:'([^']+)',\s+content:'([^']+)'\s*\},",
            RegexOptions.Singleline
        );
        
        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");
        
        _shops = new List<ShopData>();
                
        foreach (Match match in matches)
        {
            // {
            //     coords:{lat: 57.068828, lng: 22.294163},
            //     shopLogo:'https://citro.lv/wp-content/themes/citro/app/assets/img/logo-xs-mini.svg',
            //     content:'<p style="margin:0 0 5px 0;font-size:16px;"><strong>"Saktas", Rendas pag., Kuldīgas nov.</strong></p><p>Darba laiks: 8:00-22:00  Automatizēta taras pieņemšana</p>'
            // },

            string coordContent = match.Groups[1].ToString();
            // lat: 57.068828, lng: 22.294163
            double lat = double.Parse(Regex.Match(coordContent, @"lat: ([^,]+),").Groups[1].ToString());
            double lon = double.Parse(Regex.Match(coordContent, @"lng: (.+)$").Groups[1].ToString());
            
            string iconContent = match.Groups[2].ToString();
            // https://citro.lv/wp-content/themes/citro/app/assets/img/logo-xs-mini.svg
            ShopType shopType = ShopTypeFromIcon(Regex.Match(iconContent, @"\/([^\/\.]+)\.svg").Groups[1].ToString());

            string contentContent = match.Groups[3].ToString();
            // <p style="margin:0 0 5px 0;font-size:16px;"><strong>"Saktas", Rendas pag., Kuldīgas nov.</strong></p><p>Darba laiks: 8:00-22:00  Automatizēta taras pieņemšana</p>
            string address = Regex.Match(contentContent, @"<strong>(.+)</strong>").Groups[1].ToString();

            _shops.Add(
                new ShopData(
                    "Citro" + (shopType == ShopType.Mini ? " MINI" : ""),
                    address,
                    new OsmCoord(lat, lon)
                )
            );
        }
    }
    
    private ShopType ShopTypeFromIcon(string iconName)
    {
        return iconName switch
        {
            "logo-xs"    => ShopType.Regular,
            "logo-xs-mini" => ShopType.Mini,

            _ => throw new ArgumentOutOfRangeException(nameof(iconName), iconName, null)
        };
    }

    
    private enum ShopType
    {
        Regular,
        Mini
    }
}