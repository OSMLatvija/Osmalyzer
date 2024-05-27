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

    // List of standalone Deposit kiosks
    public List<AutomatedDepositLocation> DepositKiosk { get; private set; } = null!; // only null before prepared

    // List of shops that accept bottles at counter
    public List<ManualDepositLocation> ManualDepositLocations { get; private set; } = null!; // only null before prepared

    // List of taromats, that accept bottles. Both in kiosks and inside shops
    public List<DepositAutomat> DepositAutomats { get; private set; } = null!; // only null before prepared


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
        DepositKiosk = new List<AutomatedDepositLocation>();
        ManualDepositLocations = new List<ManualDepositLocation>();
        DepositAutomats = new List<DepositAutomat>();

        string source = File.ReadAllText(DataFileName);

        // Match objects for deposit locations (all types)
        MatchCollection matches = Regex.Matches(
            source,
            @"\{""type"":""Feature"",""properties"":\{(?<properties>[^{}]*)\},""geometry"":\{(?<geometry>[^{}]*)\}[^{}]*\}" //more magic regexes
        );

        if (matches.Count == 0)
            throw new Exception("Did not match any items on webpage");

        foreach (Match match in matches)
        {
            // Parse fields that are of interest for us
            string properties = match.Groups["properties"].ToString();
            string geometry = match.Groups["geometry"].ToString();

            string dioId = Regex.Unescape(Regex.Match(properties, @"""uni_id"":""([^""]+)""").Groups[1].ToString());
            string address = Regex.Unescape(Regex.Match(properties, @"""Adrese"":""((?:\\""|[^""])*)""").Groups[1].ToString());
            string shopName = Regex.Unescape(Regex.Match(properties, @"""Shop_name"":""((?:\\""|[^""])*)""").Groups[1].ToString());
            string numberOfTaromats = Regex.Unescape(Regex.Match(properties, @"""taromata_tips"":""((?:\\""|[^""])*)""").Groups[1].ToString()).ToLower();
            string mode = Regex.Match(properties, @"""AutoManual"":""([ABMabm])(?:utomat[^""]*)?""").Groups[1].ToString();

            Match geometryMatch = Regex.Match(geometry, @"""coordinates"":\[(?<long>\d+\.\d+),(?<lat>\d+\.\d+)\]");
            double lat = double.Parse(Regex.Unescape(geometryMatch.Groups["lat"].ToString()), CultureInfo.InvariantCulture);
            double lon = double.Parse(Regex.Unescape(geometryMatch.Groups["long"].ToString()), CultureInfo.InvariantCulture);

            if (mode.Equals("M") || numberOfTaromats.Contains("manuālā"))  // because data is inconsistent: uni_id=51666
            {
                ManualDepositLocation location = new ManualDepositLocation(
                    dioId,
                    address,
                    shopName,
                    new OsmCoord(lat, lon)
                );
                ManualDepositLocations.Add(location);
            }
            else
            {
                AutomatedDepositLocation location = new AutomatedDepositLocation(
                    dioId,
                    address,
                    shopName,
                    new OsmCoord(lat, lon)
                );

                // Only want standalone kiosks. No way of knowing that, so trying to guess
                if (
                    !shopName.ToLower().Contains("lidl") // AFAIK, Lidl has only taromats inside
                    && Regex.Match(numberOfTaromats,@"liel(?:ais|ie) taromāt[si]").Success) // AFAIK, kiosks only have big taromats
                {
                    DepositKiosk.Add(location);
                }

                // For automated points with info about number of automats provided, add automats into a separate list
                if (!string.IsNullOrWhiteSpace(numberOfTaromats))
                {
                    // Try to parse number of taromats https://regex101.com/r/8CEDfv/1
                    MatchCollection matchedTaromats = Regex.Matches(
                        numberOfTaromats,
                        @"(?:(?<a_num>\d+) )?(?:mazais|vidējais|liel(?:ais|ie)) (?<taromat>taromāt[si])|(?:(?<b_num>\d+) )?(?<beram>beramtaromāt[si])"
                    );

                    if (matchedTaromats.Count == 0)
                        throw new Exception("Didn't recognise number of taromats: '" + numberOfTaromats + "'");

                    foreach (Match matchedTaromat in matchedTaromats)
                    {                   
                        if (!string.IsNullOrEmpty(matchedTaromat.Groups["beram"]?.Value))
                        {
                            var taromat = new DepositAutomat(location, TaromatMode.BeramTaromat);
                            int number = int.TryParse(matchedTaromat.Groups["b_num"]?.Value, out int b_num) ? b_num : 1;
                            for (int i = 0; i < number; i++) 
                            {
                                DepositAutomats.Add(taromat);
                            }
                        }
                        else if (!string.IsNullOrEmpty(matchedTaromat.Groups["taromat"]?.Value))
                        {
                            var taromat = new DepositAutomat(location, TaromatMode.Taromat);
                            int number = int.TryParse(matchedTaromat.Groups["a_num"]?.Value, out int a_num) ? a_num : 1;
                            for (int i = 0; i < number; i++) 
                            {
                                DepositAutomats.Add(taromat);
                            }
                        }
                    }
                }
            }
        }
    }
}