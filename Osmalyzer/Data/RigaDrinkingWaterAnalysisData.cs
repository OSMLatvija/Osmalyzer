using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class RigaDrinkingWaterAnalysisData : AnalysisData, IPreparableAnalysisData, IUndatedAnalysisData
{
    public override string Name => "Riga Drinking Water";

    protected override string DataFileIdentifier => "riga-drinking-water";


    public List<DrinkingWater> DrinkingWaters { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        WebsiteDownloadHelper.Download(
            "https://www.rigasudens.lv/lv/udens-brivkranu-karte", 
            cacheBasePath + DataFileIdentifier + @".html"
        );
    }

    public void Prepare()
    {
        DrinkingWaters = new List<DrinkingWater>();

        string text = File.ReadAllText(cacheBasePath + DataFileIdentifier + @".html");

        MatchCollection matches = Regex.Matches(text, @"{""id"":""[^""]+"",""name"":""([^""]+)"",""type"":""([^""]+)"",""latitude"":""([^""]+)"",""longitude"":""([^""]+)""}");

        if (matches.Count == 0) throw new InvalidOperationException();
            
        foreach (Match match in matches)
        {
            string name = Regex.Unescape(match.Groups[1].ToString()); // ok the source isn't encoded for regex but it works - I have unicode stuff like "mobil\u0101 \u016bdens"
            DrinkingWater.InstallationType type = TypeStringToType(match.Groups[2].ToString());
            double latitude = double.Parse(match.Groups[3].ToString());
            double longitude = double.Parse(match.Groups[4].ToString());

            DrinkingWaters.Add(new DrinkingWater(name, type, new OsmCoord(latitude, longitude)));
        }
    }

    [Pure]
    private static DrinkingWater.InstallationType TypeStringToType(string s)
    {
        return s switch
        {
            "static" => DrinkingWater.InstallationType.Static,
            "mobile" => DrinkingWater.InstallationType.Mobile,
            _        => throw new NotImplementedException()
        };
    }
}