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


    public List<GenericData> VendingMachines { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        string infoPageText = WebsiteDownloadHelper.Read(ReportWebLink, true);
            
        Match mapMatch = Regex.Match(infoPageText, @"src=""https://www\.google\.com/maps/d/embed\?mid=([a-zA-Z0-9_.]+)&");
        // https://www.google.com/maps/d/viewer?mid=1wRS7q3l_ESgCVKjHm1lO_dW0o3rSJYU

        string mapId = mapMatch.Groups[1].ToString();

        string kmlUrl = $@"https://www.google.com/maps/d/kml?mid={mapId}&forcekml=1";
        // forcekml to be readable xml kml and not "encoded" kmd
        // https://www.google.com/maps/d/kml?mid=1wRS7q3l_ESgCVKjHm1lO_dW0o3rSJYU&forcekml=1
            
        WebsiteDownloadHelper.Download(
            kmlUrl, 
            Path.Combine(CacheBasePath, DataFileIdentifier + @".kml")
        );
    }

    protected override void DoPrepare()
    {
        VendingMachines = new List<GenericData>();

        using FileStream fileStream = File.OpenRead(Path.Combine(CacheBasePath, DataFileIdentifier + @".kml"));
            
        KmlFile kmlFile = KmlFile.Load(fileStream);

        IEnumerable<Placemark> placemarks = kmlFile.Root.Flatten().OfType<Placemark>();
            
        foreach (Placemark placemark in placemarks)
        {
            if (placemark.Geometry is Point point)
            {                    
                VendingMachines.Add(
                    new GenericData(
                        new OsmCoord(point.Coordinate.Latitude, point.Coordinate.Longitude),
                        placemark.Description?.Text ?? "",
                        placemark.Name,  // name is address in this list for some reason
                        "Ticket vending machine"
                    )
                );
            }
        }
    }
}