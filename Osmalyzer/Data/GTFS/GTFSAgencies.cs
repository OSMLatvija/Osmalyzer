namespace Osmalyzer;

/// <summary>
/// "Agency information such as operator name and website."
/// </summary>
public class GTFSAgencies
{
    public IEnumerable<GTFSAgency> Agencies => _agencies.Values.AsEnumerable();

    
    private readonly Dictionary<string, GTFSAgency> _agencies;

    
    public GTFSAgencies(string dataFileName)
    {
        _agencies = new Dictionary<string, GTFSAgency>(StringComparer.OrdinalIgnoreCase);
        
        Parse(dataFileName);
    }

    
    [Pure]
    public GTFSAgency? GetAgency(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _agencies.TryGetValue(id, out GTFSAgency? agency) ? agency : null;
    }

    
    private void Parse(string dataFileName)
    {
        if (!File.Exists(dataFileName))
            return;

        string[] lines = File.ReadAllLines(dataFileName);

        if (lines.Length < 2)
            return;

        string[] headers = lines[0].Split(',').Select(s => s.Trim()).ToArray();

        int agencyIdIndex = Array.IndexOf(headers, "agency_id");
        int agencyNameIndex = Array.IndexOf(headers, "agency_name");
        int agencyUrlIndex = Array.IndexOf(headers, "agency_url");
        int agencyTimezoneIndex = Array.IndexOf(headers, "agency_timezone");
        int agencyLangIndex = Array.IndexOf(headers, "agency_lang");
        int agencyPhoneIndex = Array.IndexOf(headers, "agency_phone");
        int agencyFareUrlIndex = Array.IndexOf(headers, "agency_fare_url");
        int agencyEmailIndex = Array.IndexOf(headers, "agency_email");

        if (agencyNameIndex == -1) throw new Exception($"Missing required column 'agency_name' in {dataFileName}");
        if (agencyUrlIndex == -1) throw new Exception($"Missing required column 'agency_url' in {dataFileName}");
        if (agencyTimezoneIndex == -1) throw new Exception($"Missing required column 'agency_timezone' in {dataFileName}");

        for (int i = 1; i < lines.Length; i++)
        {
            string row = lines[i];
            List<string> segments = row.Split(',').Select(s => s.Trim()).ToList();

            if (segments.Count <= agencyNameIndex || segments.Count <= agencyUrlIndex || segments.Count <= agencyTimezoneIndex)
                continue;

            string agencyId = agencyIdIndex != -1 && agencyIdIndex < segments.Count ? GetValue(segments[agencyIdIndex]) : GetValue(segments[agencyNameIndex]);
            string agencyName = GetValue(segments[agencyNameIndex]);
            string agencyUrl = GetValue(segments[agencyUrlIndex]);
            string agencyTimezone = GetValue(segments[agencyTimezoneIndex]);

            if (string.IsNullOrWhiteSpace(agencyName) || string.IsNullOrWhiteSpace(agencyUrl) || string.IsNullOrWhiteSpace(agencyTimezone))
                continue;

            string? agencyLanguage = null;
            if (agencyLangIndex != -1 && agencyLangIndex < segments.Count)
                agencyLanguage = GetValue(segments[agencyLangIndex]);

            string? agencyPhone = null;
            if (agencyPhoneIndex != -1 && agencyPhoneIndex < segments.Count)
                agencyPhone = GetValue(segments[agencyPhoneIndex]);

            string? agencyFareUrl = null;
            if (agencyFareUrlIndex != -1 && agencyFareUrlIndex < segments.Count)
                agencyFareUrl = GetValue(segments[agencyFareUrlIndex]);

            string? agencyEmail = null;
            if (agencyEmailIndex != -1 && agencyEmailIndex < segments.Count)
                agencyEmail = GetValue(segments[agencyEmailIndex]);

            GTFSAgency agency = new GTFSAgency(
                agencyId,
                agencyName,
                agencyUrl,
                agencyTimezone,
                agencyLanguage,
                agencyPhone,
                agencyFareUrl,
                agencyEmail
            );

            _agencies.TryAdd(agency.Id, agency);
        }
    }

    [Pure]
    private static string GetValue(string value)
    {
        if (value.Length <= 2)
            return value;

        if (value.StartsWith('"') && value.EndsWith('"'))
            return value.Substring(1, value.Length - 2).Replace("\"\"", "\"");

        return value;
    }
}

