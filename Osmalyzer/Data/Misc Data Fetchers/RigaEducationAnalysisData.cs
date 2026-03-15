using System.Globalization;

namespace Osmalyzer;

[UsedImplicitly]
public class RigaEducationAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Riga Education Institutions";

    public override string ReportWebLink => @"https://katalogs-iksd.riga.lv/lv/dazadi/atvertie-dati";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "riga-education";


    private string SchoolsFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @"-schools.csv");

    private string PreschoolsFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @"-preschools.csv");


    public List<RigaEducationData> Institutions { get; private set; } = null!; // only null before prepared


    protected override void Download()
    {
        // Direct CSV downloads from the IKSD open data portal
        WebsiteDownloadHelper.Download(
            "https://katalogs-iksd.riga.lv/lv/open-data-29/opendata-skolas",
            SchoolsFileName
        );

        WebsiteDownloadHelper.Download(
            "https://katalogs-iksd.riga.lv/lv/open-data-29/opendata-pirmsskolas",
            PreschoolsFileName
        );
    }

    protected override void DoPrepare()
    {
        Institutions = [];

        ParseFile(SchoolsFileName, RigaEducationType.School);
        ParseFile(PreschoolsFileName, RigaEducationType.Preschool);
    }


    private void ParseFile(string fileName, RigaEducationType type)
    {
        if (!File.Exists(fileName))
            throw new Exception("Expected education data file not found: " + fileName);

        // CSV format (no header row, semicolon-separated):
        // 0: row number (e.g. "1.")
        // 1: name
        // 2: registration number
        // 3: email
        // 4: phone(s)
        // 5: principal
        // 6: legal address
        // 7: legal address coords ("lat, lon")
        // 8: actual address(es) (can have multiple newline-separated values in one field)
        // 9: actual address coords ("lat, lon", can have multiple newline-separated)
        // 10: district
        // 11: region
        // 12: type
        // ...

        string source = File.ReadAllText(fileName);

        List<string[]> records = CsvParser.ParseAll(source, ';');

        foreach (string[] fields in records)
        {
            // Valid rows start with a row number like "1." or "42."
            if (fields.Length < 10 || !Regex.IsMatch(fields[0], @"^\d+\.$"))
                continue;

            string name = fields[1].Trim();

            if (string.IsNullOrEmpty(name))
                continue;

            string address = fields[6].Trim(); // legal address
            string coordString = fields[7].Trim(); // legal address coords

            OsmCoord coord = ParseCoordPair(coordString);

            Institutions.Add(new RigaEducationData(name, address, type, coord));
        }
    }


    /// <summary>
    /// Parse coordinate string in "lat, lon" format
    /// </summary>
    [Pure]
    private static OsmCoord ParseCoordPair(string coordString)
    {
        if (string.IsNullOrWhiteSpace(coordString))
            return new OsmCoord(0, 0);

        // Take only the first line if there are multiple coordinate pairs
        int newlineIndex = coordString.IndexOf('\n');

        if (newlineIndex >= 0)
            coordString = coordString.Substring(0, newlineIndex).Trim();

        string[] parts = coordString.Split(',');

        if (parts.Length < 2)
            return new OsmCoord(0, 0);

        if (double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
            double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
        {
            if (lat != 0 && lon != 0)
                return new OsmCoord(lat, lon);
        }

        return new OsmCoord(0, 0);
    }
}


