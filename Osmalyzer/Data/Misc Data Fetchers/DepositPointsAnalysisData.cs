using System.Globalization;

namespace Osmalyzer;

[UsedImplicitly]
public class DepositPointsAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Deposit points";

    public override string ReportWebLink => @"https://depozitapunkts.lv/lv#kur-atgriezt";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "deposit-points";

    
    private string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");

    
    /// <summary>
    /// List of standalone Deposit kiosks/buildings with one or more "taromāti" inside
    /// </summary>
    public List<KioskDepositPoint> Kiosks { get; private set; } = null!; // only null before prepared

    /// <summary>
    /// List of shops that accept bottles at counter manually
    /// </summary>
    public List<ManualDepositPoint> ManualLocations { get; private set; } = null!; // only null before prepared

    /// <summary>
    /// List of "taromāts" that accept bottles. Both in kiosks and inside shops
    /// </summary>
    public List<VendingMachineDepositPoint> VendingMachines { get; private set; } = null!; // only null before prepared


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
        Kiosks = new List<KioskDepositPoint>();
        ManualLocations = new List<ManualDepositPoint>();
        VendingMachines = new List<VendingMachineDepositPoint>();

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
            string? shopName = Regex.Unescape(Regex.Match(properties, @"""Shop_name"":""((?:\\""|[^""])*)""").Groups[1].ToString());
            string numberOfTaromats = Regex.Unescape(Regex.Match(properties, @"""taromata_tips"":""((?:\\""|[^""])*)""").Groups[1].ToString()).ToLower();
            string mode = Regex.Match(properties, @"""AutoManual"":""([ABMabm])(?:utomat[^""]*)?""").Groups[1].ToString();

            Match geometryMatch = Regex.Match(geometry, @"""coordinates"":\[(?<long>\d+\.\d+),(?<lat>\d+\.\d+)\]");
            double lat = double.Parse(Regex.Unescape(geometryMatch.Groups["lat"].ToString()), CultureInfo.InvariantCulture);
            double lon = double.Parse(Regex.Unescape(geometryMatch.Groups["long"].ToString()), CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(shopName)) // a couple aren't specified
                shopName = null;

            if (shopName != null &&
                (shopName.ToLower() == "veikals" ||
                 shopName == "Local store")) // this is not a real store, someone used placeholder name (in English)
                shopName = null;
            // There are more values that don't look like shop names, but not hard-coding single instances

            if (mode.Equals("M") || numberOfTaromats.Contains("manuāl")  || numberOfTaromats.Contains("manual"))  // because data is inconsistent: uni_id=51666
            {
                ManualDepositPoint location = new ManualDepositPoint(
                    dioId,
                    address,
                    shopName,
                    new OsmCoord(lat, lon)
                );
                ManualLocations.Add(location);
            }
            else
            {
                KioskDepositPoint location = new KioskDepositPoint(
                    dioId,
                    address,
                    shopName,
                    new OsmCoord(lat, lon)
                );

                // Only want standalone kiosks. No way of knowing that, so trying to guess
                if (shopName != null &&
                    !shopName.ToLower().Contains("lidl") // AFAIK, Lidl has only taromats inside
                    && Regex.Match(numberOfTaromats,@"liel(?:ais|ie) taromāt[si]").Success) // AFAIK, kiosks only have big taromats
                {
                    Kiosks.Add(location);
                }

                // For automated points with info about number of automats provided, add automats into a separate list
                if (!string.IsNullOrWhiteSpace(numberOfTaromats))
                {
                    // Try to parse number of taromats https://regex101.com/r/8CEDfv/1
                    MatchCollection matchedTaromats = Regex.Matches(
                        numberOfTaromats,
                        @"(?:(?<a_num>\d+) )?(?<taromat>(?:mazais|vidējais|liel(?:ais|ie))(?: taromāt[si])?)|(?:(?<b_num>\d+) )?(?<beram>beramtaromāt[si])"
                    );

                    if (matchedTaromats.Count == 0)
                        Console.WriteLine("Didn't recognise number of taromats in line '" + properties + "'");

                    foreach (Match matchedTaromat in matchedTaromats)
                    {                   
                        if (!string.IsNullOrEmpty(matchedTaromat.Groups["beram"]?.Value))
                        {
                            VendingMachineDepositPoint? taromat = new VendingMachineDepositPoint(location, TaromatMode.BeramTaromat);
                            int number = int.TryParse(matchedTaromat.Groups["b_num"]?.Value, out int b_num) ? b_num : 1;
                            for (int i = 0; i < number; i++) 
                            {
                                VendingMachines.Add(taromat);
                            }
                        }
                        else if (!string.IsNullOrEmpty(matchedTaromat.Groups["taromat"]?.Value))
                        {
                            VendingMachineDepositPoint? taromat = new VendingMachineDepositPoint(location, TaromatMode.Taromat);
                            int number = int.TryParse(matchedTaromat.Groups["a_num"]?.Value, out int a_num) ? a_num : 1;
                            for (int i = 0; i < number; i++) 
                            {
                                VendingMachines.Add(taromat);
                            }
                        }
                    }
                }
            }
        }
    }
}