﻿using SharpKml.Dom;
using SharpKml.Engine;

namespace Osmalyzer;

[UsedImplicitly]
public class RigasSatiksmeVendingAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Rīgas Satiksme ticket vending machines";

    public override string ReportWebLink => @"https://www.rigassatiksme.lv/lv/biletes/bilesu-tirdzniecibas-vietas/tirdzniecibas-vietas/";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "riga-satiksme-ticket-vending";


    public List<TicketVendingMachine> VendingMachines { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        string mapId = GetMapId();
        

        string kmlUrl = $@"https://www.google.com/maps/d/kml?mid={mapId}&forcekml=1";
        // forcekml to be readable xml kml and not "encoded" kmd
        // https://www.google.com/maps/d/kml?mid=1fHZLaJ1t5cPs9PbaUotV_-IlwVs&forcekml=1

        try
        {
            WebsiteDownloadHelper.Download(
                kmlUrl,
                Path.Combine(CacheBasePath, DataFileIdentifier + @".kml")
            );
        }
        catch (Exception e)
        {
            throw new Exception("Failed to read Google Maps kml page (" + kmlUrl + ")", e);
        }

        return;

        
        string GetMapId()
        {
            string infoPageText;

            try
            {
                infoPageText = WebsiteBrowsingHelper.Read( // direct fails on GitHub
                    ReportWebLink,
                    true
                );
            }
            catch (Exception)
            {
                // RS site is geoblocked in US, where GitHub runner is,
                // so fail gracefully and just hard-code the ID, since we are only checking the site for the id anyway
                return "zyg34wpl1-Bk.kKd944OiyVNA";
                //throw new Exception("Failed to read RS page", e);
            }

            Match mapMatch = Regex.Match(infoPageText, @"src=""https://www\.google\.com/maps/d/embed\?mid=([a-zA-Z0-9_\.]+)&");
            // https://www.google.com/maps/d/embed?mid=z04Qg9kXVnqk.kwqapebDyDDY&amp;output=embed
            // redirects to (RS site url is probably old)
            // https://www.google.com/maps/d/u/0/embed?output=embed&mid=1fHZLaJ1t5cPs9PbaUotV_-IlwVs

            if (!mapMatch.Success)
            {
                string dumpFileName = Path.Combine(ReportWriter.OutputPath, "RS-vending-html-dump.html");
                File.WriteAllText(dumpFileName, infoPageText);

                string headerDumpFileName = Path.Combine(ReportWriter.OutputPath, "RS-vending-header-dump.html");
                File.WriteAllLines(headerDumpFileName, WebsiteBrowsingHelper.RecentResponseHeaders);

                // todo: generic way to do this for all failed web requests?

                // RS site is geoblocked in US, where GitHub runner is,
                // so fail gracefully and just hard-code the ID, since we are only checking the site for the id anyway
                return "zyg34wpl1-Bk.kKd944OiyVNA";
                //throw new Exception("Couldn't parse RS site html for the Google Maps KML ID (saved html dump in output 'RS-vending-html-dump.html' and headers in 'RS-vending-header-dump.html')");
            }

            return mapMatch.Groups[1].ToString();
        }
    }

    protected override void DoPrepare()
    {
        VendingMachines = new List<TicketVendingMachine>();

        using FileStream fileStream = File.OpenRead(Path.Combine(CacheBasePath, DataFileIdentifier + @".kml"));
            
        KmlFile kmlFile = KmlFile.Load(fileStream);

        IEnumerable<Placemark> placemarks = kmlFile.Root.Flatten().OfType<Placemark>();
            
        foreach (Placemark placemark in placemarks)
        {
            if (placemark.Geometry is Point point)
            {
                if (placemark.Name != "Biļešu automāts")
                    continue; // map includes all the shops that sell tickets, not just vending machines
                
                (string? address, string? location) = ExtractLocation(placemark.Description?.Text);
                
                VendingMachines.Add(
                    new TicketVendingMachine(
                        new OsmCoord(point.Coordinate.Latitude, point.Coordinate.Longitude),
                        location,
                        address
                    )
                );
            }
        }
    }

    
    [Pure]
    private static (string? address, string? location) ExtractLocation(string? value)
    {
        if (value == null)
            return (null, null);
        
        // 13. janvāra iela, Rīga     Biļešu automāts atrodas Prāgas-Vaļņu ielas tunelī, Vecrīgas pusē

        Match match = Regex.Match(value, @"(?<addr>.+?)\s+Biļešu automāts atrodas (?<loc>.+)");
        
        // Valdeķu iela 56, Rīga. <br>Biļešu automāts atrodas veikalā "Mego"
        
        if (!match.Success)
            match = Regex.Match(value, @"(?<addr>.+?)\s?<br>\s?Biļešu automāts atrodas (?<loc>.+)");

        // Biļešu automāts atrodas netālu no slimnīcas “Gaiļezers” galvenās ieejas, pie lauksaimnieku tirgus vietām <br>Hipokrāta iela 2
        
        if (!match.Success)
            match = Regex.Match(value, @"Biļešu automāts atrodas (?<loc>.+?)\s?<br>(?<addr>.+)");
        
        // Nometņu iela 64 automāts atrodas p/v "Āgenskalna tirgus"
        
        if (!match.Success)
            match = Regex.Match(value, @"(?<addr>.+?)\s+automāts atrodas (?<loc>.+)");
        
        if (!match.Success)
            return (null, value);
        
        return (match.Groups["addr"].ToString(), match.Groups["loc"].ToString());
    }
}