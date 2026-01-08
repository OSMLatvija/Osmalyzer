using WikidataSharp;

namespace Osmalyzer;

/// <summary>
/// Wikidata entries for Latvian cities including state cities and regional cities
/// </summary>
[UsedImplicitly]
public class CitiesWikidataData : WikidataData
{
    public override string Name => "Cities Wikidata";

    public override string ReportWebLink => @"https://www.wikidata.org/wiki/Q" + stateCityOfLatviaQID;

    public override bool NeedsPreparation => true;


    private const long stateCityOfLatviaQID = 109329953;
    private const long cityUnderMunicipalityJurisdictionQID = 15584664;


    protected override string DataFileIdentifier => "cities-wikidata";


    private string StateCitiesRawFilePath => Path.Combine(CacheBasePath, DataFileIdentifier + "-state-raw.json");
    private string RegionalCitiesRawFilePath => Path.Combine(CacheBasePath, DataFileIdentifier + "-regional-raw.json");


    public List<WikidataItem> AllCities { get; private set; } = null!; // only null before prepared
    public List<WikidataItem> StateCities { get; private set; } = null!; // only null before prepared
    public List<WikidataItem> RegionalCities { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        // Fetch state cities (e.g., Jelgava)
        string stateCitiesRaw = Wikidata.FetchItemsByInstanceOfRaw(stateCityOfLatviaQID);
        File.WriteAllText(StateCitiesRawFilePath, stateCitiesRaw);

        // Fetch regional cities (e.g., Ikšķile)
        string regionalCitiesRaw = Wikidata.FetchItemsByInstanceOfRaw(cityUnderMunicipalityJurisdictionQID);
        File.WriteAllText(RegionalCitiesRawFilePath, regionalCitiesRaw);
        
        // Process immediately after download
        ProcessDownloadedData();
    }

    protected override void DoPrepare()
    {
        // Load from cached files and process
        ProcessDownloadedData();
    }

    
    private void ProcessDownloadedData()
    {
        string stateCitiesRaw = File.ReadAllText(StateCitiesRawFilePath);
        StateCities = Wikidata.ProcessItemsByInstanceOfRaw(stateCitiesRaw);
        if (StateCities.Count == 0) throw new Exception("No state cities were fetched from Wikidata.");

        StateCities = FilterOutDissolved(StateCities);

        string regionalCitiesRaw = File.ReadAllText(RegionalCitiesRawFilePath);
        RegionalCities = Wikidata.ProcessItemsByInstanceOfRaw(regionalCitiesRaw);
        if (RegionalCities.Count == 0) throw new Exception("No regional cities were fetched from Wikidata.");

        RegionalCities = FilterOutDissolved(RegionalCities);

        AllCities = [ ];
        AllCities.AddRange(StateCities);
        AllCities.AddRange(RegionalCities);
        
#if DEBUG
        // foreach (WikidataItem item in StateCities) Debug.WriteLine($"State City: \"{item.GetLabel("lv")}\" ({item.QID}) w/ {item.Statements.Count} statements");
        // foreach (WikidataItem item in RegionalCities) Debug.WriteLine($"Regional City: \"{item.GetLabel("lv")}\" ({item.QID}) w/ {item.Statements.Count} statements");
#endif
    }



    public void Assign<T>(List<T> dataItems, Func<T, WikidataItem, bool> matcher, out List<WikidataMatchIssue> multiMatches)
        where T : class, IHasWikidataItem
    {
        AssignWikidataItems(dataItems, AllCities, matcher, out multiMatches);
    }
}


