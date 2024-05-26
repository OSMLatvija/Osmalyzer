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

    // List of Depozita punkts. Might be standalone building, amenity inside shop or shop itself, that accepts bottles
    public List<DepositPoint> DepositPoints { get; private set; } = null!; // only null before prepared

    // List of automats, that accept bottles.
    public List<DepositPoint.DepositAutomat> DepositAutomats { get; private set; } = null!; // only null before prepared


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
        DepositAutomats = new List<DepositPoint.DepositAutomat>();

        string source = File.ReadAllText(DataFileName);

        MatchCollection matches = Regex.Matches(
            source,
            @"\{""type"":""Feature"",""properties"":\{(?<properties>[^{}]*)\},""geometry"":\{(?<geometry>[^{}]*)\}[^{}]*\}" //more magic regexes
        );

        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");

        foreach (Match match in matches)
        {
            string properties = match.Groups["properties"].ToString();
            string geometry = match.Groups["geometry"].ToString();

            string dioId = Regex.Unescape(Regex.Match(properties, @"""uni_id"":""([^""]+)""").Groups[1].ToString());
            string address = Regex.Unescape(Regex.Match(properties, @"""Adrese"":""((?:\\""|[^""])*)""").Groups[1].ToString());
            string shopName = Regex.Unescape(Regex.Match(properties, @"""Shop_name"":""((?:\\""|[^""])*)""").Groups[1].ToString());
            string numberOfTaromats = Regex.Unescape(Regex.Match(properties, @"""taromata_tips"":""((?:\\""|[^""])*)""").Groups[1].ToString()).ToLower();
            DepositPoint.DepositPointMode mode = ModeStringToType(Regex.Match(properties, @"""AutoManual"":""([ABMabm])(?:utomat[^""]*)?""").Groups[1].ToString());

            Match geometryMatch = Regex.Match(geometry, @"""coordinates"":\[(?<long>\d+\.\d+),(?<lat>\d+\.\d+)\]");
            double lat = double.Parse(Regex.Unescape(geometryMatch.Groups["lat"].ToString()), CultureInfo.InvariantCulture);
            double lon = double.Parse(Regex.Unescape(geometryMatch.Groups["long"].ToString()), CultureInfo.InvariantCulture);


            DepositPoint point = new DepositPoint(
                    dioId,
                    address,
                    shopName,
                    mode,
                    new OsmCoord(lat, lon)
                );

            DepositPoints.Add(point);
            
            // if deposit point is automated and info about number of automats provided, add automats into a separate list
            if (point.Mode != DepositPoint.DepositPointMode.Manual
                    && !numberOfTaromats.Contains("manuālā")  // because data is inconsistent
                    && !string.IsNullOrWhiteSpace(numberOfTaromats))
            {
                MatchCollection matchedTaromats = Regex.Matches(
                    numberOfTaromats,
                    @"(?:(?<a_num>\d+) )?(?:mazais|vidējais|liel(?:ais|ie)) (?<taromat>taromāt[si])|(?:(?<b_num>\d+) )?(?<beram>beramtaromāt[si])"
                );

                if (matchedTaromats.Count == 0)
                    throw new Exception("Didn't recognise number of taromats: '" + numberOfTaromats + "'");

                foreach (Match matchedTaromat in matchedTaromats)
                {
                    var taromat = new DepositPoint.DepositAutomat(point);
                    
                    if (!string.IsNullOrEmpty(matchedTaromat.Groups["beram"]?.Value))
                    {
                        int number = int.TryParse(matchedTaromat.Groups["b_num"]?.Value, out int b_num) ? b_num : 1;
                        taromat.Mode = DepositPoint.DepositPointMode.BeramTaromats;
                        for (int i = 0; i < number; i++) 
                        {
                            DepositAutomats.Add(taromat);
                        }
                    }
                    else if (!string.IsNullOrEmpty(matchedTaromat.Groups["taromat"]?.Value))
                    {
                        int number = int.TryParse(matchedTaromat.Groups["a_num"]?.Value, out int a_num) ? a_num : 1;
                        taromat.Mode = DepositPoint.DepositPointMode.Automatic;
                        for (int i = 0; i < number; i++) 
                        {
                            DepositAutomats.Add(taromat);
                        }
                    }
                }
            }
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
            _ => throw new NotImplementedException()
        };
    }
}