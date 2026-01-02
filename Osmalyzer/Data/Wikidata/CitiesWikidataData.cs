using System.Diagnostics;
using WikidataSharp;

namespace Osmalyzer;

/// <summary>
/// Wikidata entries for Latvian cities including state cities and regional cities
/// </summary>
[UsedImplicitly]
public class CitiesWikidataData : AdminWikidataData
{
    public override string Name => "Cities Wikidata";

    public override string ReportWebLink => @"https://www.wikidata.org/wiki/Q" + stateCityOfLatviaQID;

    public override bool NeedsPreparation => false;


    private const long stateCityOfLatviaQID = 109329953;
    private const long cityUnderMunicipalityJurisdictionQID = 15584664;


    protected override string DataFileIdentifier => "cities-wikidata";


    public List<WikidataItem> StateCities { get; private set; } = null!; // only null before prepared
    public List<WikidataItem> RegionalCities { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        // Fetch state cities (e.g., Jelgava)
        StateCities = Wikidata.FetchItemsByInstanceOf(stateCityOfLatviaQID);
        if (StateCities.Count == 0) throw new Exception("No state cities were fetched from Wikidata.");

        // Fetch regional cities (e.g., Ikšķile)
        RegionalCities = Wikidata.FetchItemsByInstanceOf(cityUnderMunicipalityJurisdictionQID);
        if (RegionalCities.Count == 0) throw new Exception("No regional cities were fetched from Wikidata.");

#if DEBUG
        foreach (WikidataItem item in StateCities) Debug.WriteLine($"State City: \"{item.GetLabel("lv")}\" ({item.QID}) w/ {item.Statements.Count} statements");
        foreach (WikidataItem item in RegionalCities) Debug.WriteLine($"Regional City: \"{item.GetLabel("lv")}\" ({item.QID}) w/ {item.Statements.Count} statements");
#endif
    }

    protected override void DoPrepare()
    {
        throw new InvalidOperationException();
    }


    public void Assign<T>(List<T> dataItems, Func<T, string> dataItemNameLookup, Action<T, WikidataItem> dataItemAssigner)
    {
        List<WikidataItem> allCities = [ ];
        allCities.AddRange(StateCities);
        allCities.AddRange(RegionalCities);

        AssignWikidataItems(dataItems, allCities, dataItemNameLookup, dataItemAssigner);
    }
}


