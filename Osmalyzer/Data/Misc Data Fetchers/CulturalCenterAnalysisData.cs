
using System.Globalization;

namespace Osmalyzer;

[UsedImplicitly]
public class CulturalCenterAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Cultural Centers";

    public override string ReportWebLink => @"https://data.gov.lv/dati/lv/dataset/kulturas-centru-statistika/resource/f08a670c-dfab-44db-9ac7-fc3303182af3";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "cultural-centers";


    public List<CulturalCenterData> CulturalCenters { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        string result = WebsiteBrowsingHelper.Read( // data.gov.lv seems to not like direct reading/scraping
            ReportWebLink,
            true
        );

        // The dropdown on the resource page has a CKAN datastore CSV dump link:
        // <a class="dropdown-item" href="/dati/lv/datastore/dump/{id}?bom=True">CSV</a>
        Match urlMatch = Regex.Match(result, @"href=""(/dati/lv/datastore/dump/[^""]+\?bom=True)""");

        if (!urlMatch.Success)
            throw new Exception("Could not find the datastore CSV dump URL for cultural center data on data.gov.lv page.");

        string url = "https://data.gov.lv" + urlMatch.Groups[1].Value;

        WebsiteBrowsingHelper.DownloadPage( // data.gov.lv seems to not like direct download
            url,
            Path.Combine(CacheBasePath, DataFileIdentifier + @".csv")
        );
    }

    protected override void DoPrepare()
    {
        CulturalCenters = [];

        string dataFileName = Path.Combine(CacheBasePath, DataFileIdentifier + @".csv");

        if (!File.Exists(dataFileName))
            throw new Exception("Expected cultural center data file not found: " + dataFileName);

        string source = File.ReadAllText(dataFileName, Encoding.UTF8);

        List<string[]> records = CsvParser.ParseAll(source, ',');

        if (records.Count == 0)
            throw new Exception("No records found in cultural center CSV.");

        // Validate header
        // Fields: _id,Objekta nosaukums,ISIL kods,Adrese,LAT,LON,LKS-92 X,LKS-92 Y,
        //         Vadītājs/ direktors,Juridiskais statuss,Dibināšanas gads,Sākums,Misija,
        //         Darbība,Bezmaksas Wi-Fi,...,Darbojās 2022.gadā,...
        string[] header = records[0];

        if (header.Length < 6 ||
            header[0] != "_id" ||
            header[1] != "Objekta nosaukums" ||
            header[3] != "Adrese" ||
            header[4] != "LAT" ||
            header[5] != "LON")
            throw new Exception(
                "Unexpected CSV header - cultural center data format may have changed. " +
                "First fields: " + string.Join(", ", header.Take(6).Select(h => "`" + h + "`"))
            );

        for (int i = 1; i < records.Count; i++)
        {
            string[] fields = records[i];

            if (fields.Length < 6)
                continue;

            string name = fields[1].Trim();

            if (string.IsNullOrEmpty(name))
                continue;

            string address = fields[3].Trim();

            OsmCoord coord = new OsmCoord(0, 0);

            if (double.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) &&
                lat != 0 && lon != 0)
            {
                coord = new OsmCoord(lat, lon);
            }

            CulturalCenters.Add(new CulturalCenterData(name, address, coord));
        }

        if (CulturalCenters.Count == 0)
            throw new Exception("Failed to parse any cultural centers from data.");
    }
}
