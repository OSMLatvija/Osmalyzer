using System;
using System.Collections.Generic;
using System.Globalization;
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
            @"\{""type"":""Feature"",""properties"":\{(?<properties>[^{}]*)\},""geometry"":\{(?<geometry>[^{}]*)\}[^{}]*\}" //more magic regexes
        );

//@"\{(?:""id"":\d+,)?""Adrese"":""(?<address>(?:\\""|[^""])*)"",""Datums"":(?:null|""(?:\\""|[^""])*""),\s*""uni_id"":""(?<id>[^""]+)"",""reitings"":(?:null|\d|""[^""]*""),""Shop_name"":(?:null|""(?<shop>(?:\\""|[^""])*)""),""AutoManual"":""(?<mode>[ABMabm])[^""]*"",""taromata_tips"":(?:null|""(?<size>(?:\\""|[^""])*)""),""Planota_metode"":(?:null|""(?:\\""|[^""])*"")(?:,""object_gauja_id"":[^}]*)?"


        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");

        foreach (Match match in matches)
        {
            string properties = match.Groups["properties"].ToString();
            string geometry = match.Groups["geometry"].ToString();

            string dioId = Regex.Unescape(Regex.Match(properties, @"""uni_id"":""([^""]+)""").Groups[1].ToString());
            string address = Regex.Unescape(Regex.Match(properties, @"""Adrese"":""((?:\\""|[^""])*)""").Groups[1].ToString());
            string shopName = Regex.Unescape(Regex.Match(properties, @"""Shop_name"":""((?:\\""|[^""])*)""").Groups[1].ToString());
            DepositPoint.DepositPointMode mode = ModeStringToType(Regex.Match(properties, @"""AutoManual"":""([ABMabm])(?:utomat[^""]*)?""").Groups[1].ToString());
            
            Match geometryMatch = Regex.Match(geometry, @"""coordinates"":\[(?<long>\d+\.\d+),(?<lat>\d+\.\d+)\]");
            double lat = double.Parse(Regex.Unescape(geometryMatch.Groups["lat"].ToString()), CultureInfo.InvariantCulture);
            double lon = double.Parse(Regex.Unescape(geometryMatch.Groups["long"].ToString()), CultureInfo.InvariantCulture);

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