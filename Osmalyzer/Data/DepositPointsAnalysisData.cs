using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class DepositPointsAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Deposit points";

    public override string ReportWebLink => @"https://depozitapunkts.lv/lv#kur-atgriezt";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "deposit-points";

    public string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");


    public List<DepositPoint> DepositPoints { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        // list at https://depozitapunkts.lv/lv#kur-atgriezt
        // query to get JSON data at GET https://projects.kartes.lv/dio_api/
    
        WebsiteDownloadHelper.Download(
            "https://projects.kartes.lv/dio_api/",
            DataFileName
        );
    }

    protected override void DoPrepare()
    {
        DepositPoints = new List<DepositPoint>();

        string source = File.ReadAllText(DataFileName);

        MatchCollection matches = Regex.Matches(
            source,
            @"\{""type"":""Feature"",""properties"":\{(?:""id"":\d+,)?""Adrese"":""(?<address>(?:\\""|[^""])*)"",""Datums"":(?:null|""(?:\\""|[^""])*""),\s*""uni_id"":""(?<id>[^""]+)"",""reitings"":(?:null|\d|""[^""]*""),""Shop_name"":(?:null|""(?<shop>(?:\\""|[^""])*)""),""AutoManual"":""(?<mode>[ABMabm])[^""]*"",""taromata_tips"":(?:null|""(?<size>(?:\\""|[^""])*)""),""Planota_metode"":(?:null|""(?:\\""|[^""])*"")(?:,""object_gauja_id"":[^}]*)?\},""geometry"":\{""type"":""Point"",""coordinates"":\[(?<long>\d+\.\d+),(?<lat>\d+\.\d+)\]\},""id"":\d+\}"
        );

        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");

        foreach (Match match in matches)
        {
            string dioId = Regex.Unescape(match.Groups["id"].ToString());
            string address = Regex.Unescape(match.Groups["address"].ToString());
            string shopName = Regex.Unescape(match.Groups["shop"].ToString());

            DepositPoint.DepositPointMode mode = ModeStringToType(match.Groups["mode"].ToString());
            double lat = double.Parse(Regex.Unescape(match.Groups["lat"].ToString()));
            double lon = double.Parse(Regex.Unescape(match.Groups["long"].ToString()));

            DepositPoints.Add(
                new DepositPoint(
                    dioId,
                    address,
                    shopName,
                    mode,
                    new OsmCoord(lat, lon)
                )
            );
        }
    }

    [Pure]
    private static DepositPoint.DepositPointMode ModeStringToType(string s)
    {
        return s.ToUpper() switch
        {
            "A" => DepositPoint.DepositPointMode.Automatic,
            "M" => DepositPoint.DepositPointMode.Manual,
            "B" => DepositPoint.DepositPointMode.BeramTaromats,
            _   => throw new NotImplementedException()
        };
    }
}