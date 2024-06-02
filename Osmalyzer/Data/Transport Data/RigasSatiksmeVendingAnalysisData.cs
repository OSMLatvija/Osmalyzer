using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SharpKml.Dom;
using SharpKml.Engine;

namespace Osmalyzer;

[UsedImplicitly]
public class RigasSatiksmeVendingAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Rīgas Satiksme ticket vending machines";

    public override string ReportWebLink => @"https://www.rigassatiksme.lv/lv/biletes/bilesu-tirdzniecibas-vietas/bilesu-automati";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "riga-satiksme-ticket-vending";


    public List<TicketVendingMachine> VendingMachines { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        string infoPageText;
        
        try
        {
            infoPageText = WebsiteBrowsingHelper.Read( // direct fails on GitHub
                ReportWebLink, 
                true
            );
        }
        catch (Exception e)
        {
            throw new Exception("Failed to read RS page", e);
        }
            
        Match mapMatch = Regex.Match(infoPageText, @"src=""https://www\.google\.com/maps/d/embed\?mid=([a-zA-Z0-9_.]+)&");
        // https://www.google.com/maps/d/viewer?mid=1wRS7q3l_ESgCVKjHm1lO_dW0o3rSJYU

        string mapId = mapMatch.Groups[1].ToString();

        string kmlUrl = $@"https://www.google.com/maps/d/kml?mid={mapId}&forcekml=1";
        // forcekml to be readable xml kml and not "encoded" kmd
        // https://www.google.com/maps/d/kml?mid=1wRS7q3l_ESgCVKjHm1lO_dW0o3rSJYU&forcekml=1

        try
        {
            WebsiteDownloadHelper.Download(
                kmlUrl,
                Path.Combine(CacheBasePath, DataFileIdentifier + @".kml")
            );
        }
        catch (Exception e)
        {
            throw new Exception("Failed to read Google Maps kml page", e);
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
                string? location = SanitizeLocation(placemark.Description?.Text);
                
                VendingMachines.Add(
                    new TicketVendingMachine(
                        new OsmCoord(point.Coordinate.Latitude, point.Coordinate.Longitude),
                        location,
                        placemark.Name // name is address in this list for some reason
                    )
                );
            }
        }
    }

    
    [Pure]
    private static string? SanitizeLocation(string? value)
    {
        if (value == null)
            return null;
        
        if (value == "Biļešu automāts") // useless value, 1 instance as of writing this
            return null;

        // All the rest seem to be prefixed with a semi-useless string, so just trim it
        
        const string prefix = "Biļešu automāts atrodas ";

        if (!value.StartsWith(prefix)) // currently, all do, but future-proof
            return value;
        
        value = value[prefix.Length..];
                            
        if (value.Length > 0)
            value = char.ToUpper(value[0]) + value[1..];
        
        // We could theoretically parse more
        // "p/v "Mežaparks"" - implcit psv stop, about half the entries are like this

        return value;
    }
}