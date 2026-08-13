namespace Osmalyzer;

/// <summary>
/// "Stops where vehicles pick up or drop off riders. Also defines stations and station entrances."
/// </summary>
public class GTFSStops
{
    public IEnumerable<GTFSStop> Stops => _stops.Values.AsEnumerable();

        
    private readonly Dictionary<string, GTFSStop> _stops;

        
    public GTFSStops(string dataFileName)
    {
        string[] lines = File.ReadAllLines(dataFileName);
        
        _stops = new Dictionary<string, GTFSStop>(StringComparer.OrdinalIgnoreCase);

        if (lines.Length < 2)
            throw new Exception($"Missing data in {dataFileName}");
        
        string header = lines[0];
        string[] headers = header.Split(',').Select(s => s.Trim()).ToArray();
        
        int stopIdIndex = headers.IndexOf("stop_id");
        int stopNameIndex = headers.IndexOf("stop_name");
        int stopLatIndex = headers.IndexOf("stop_lat");
        int stopLonIndex = headers.IndexOf("stop_lon");
        
        if (stopIdIndex == -1) throw new Exception($"Missing required column 'stop_id' in {dataFileName}");
        if (stopNameIndex == -1) throw new Exception($"Missing required column 'stop_name' in {dataFileName}");
        if (stopLatIndex == -1) throw new Exception($"Missing required column 'stop_lat' in {dataFileName}");
        if (stopLonIndex == -1) throw new Exception($"Missing required column 'stop_lon' in {dataFileName}");
        
        int stopLocationTypeIndex = Array.IndexOf(headers, "location_type");

        for (int i = 1; i < lines.Length; i++)
        {
            string row = lines[i];
            
            // stop_id,stop_code,stop_name,stop_desc,stop_lat,stop_lon,stop_url,location_type,parent_station
            // 0470,,"Tallinas iela",,56.95896,24.14143,https://saraksti.rigassatiksme.lv,,

            List<string> segments = row.Split(',').Select(s => s.Trim()).ToList();

            // stop_id - 0470
            // stop_code - 
            // stop_name - "Tallinas iela"
            // stop_desc - 
            // stop_lat - 56.95896
            // stop_lon - 24.14143
            // stop_url - https://saraksti.rigassatiksme.lv
            // location_type - 
            // parent_station - 

            string id = segments[stopIdIndex];
            string name = GetName(segments[stopNameIndex]);
            if (!double.TryParse(segments[stopLatIndex], out double lat)) 
                continue; // broken data
            if (!double.TryParse(segments[stopLonIndex], out double lon)) 
                continue; // broken data
            
            GTFSStopLocationType locationType = GTFSStopLocationType.Unknown;
            if (stopLocationTypeIndex != -1)
                locationType = GetLocationTypeFromValue(segments[stopLocationTypeIndex]);
            // For example JAP lists both stops and stop areas as "stations"
            // stop_id,stop_code,stop_name,stop_desc,stop_lat,stop_lon,zone_id,stop_url,location_type,parent_station,stop_timezone,wheelchair_boarding,platform_code
            // JAP:StopArea:835383405,,Rosība,,56.63849,23.76673,,,1,,,0,
            // JAP:ScheduledStopPoint:214,,Rosība,,56.63844,23.76668,,,0,JAP:StopArea:835383405,,0,
            // JAP:ScheduledStopPoint:215,,Rosība,,56.63854,23.76679,,,0,JAP:StopArea:835383405,,0,
            
            GTFSStop stop = new GTFSStop(id, name, new OsmCoord(lat, lon), locationType);

            _stops.TryAdd(stop.Id, stop);
            // Latvijas Autobuss has duplicates, e.g.
            // 7123k,,"Majoru stacija",,56.97155,23.79636,https://www.marsruti.lv/jurmala/index.html#stop/7123k,,
            // 7123k,,"Majori",,56.97155,23.79636,https://www.marsruti.lv/jurmala/index.html#stop/7123k,,
            // (second has paired 7123l,,"Majori",,56.97149,23.79807,https://www.marsruti.lv/jurmala/index.html#stop/7123l,,)
            // todo: report these as problems? only if coord different? store both names?
        }

        return;


        [Pure]
        static string GetName(string value)
        {
            if (value.Length <= 2) return value;
            
            if (value.StartsWith('"') && value.EndsWith('"'))
                return value.Substring(1, value.Length - 2).Replace("\"\"", "\"");
            
            return value;
        }

        [Pure]
        static GTFSStopLocationType GetLocationTypeFromValue(string value)
        {
            return value.Trim() switch
            {
                "" => GTFSStopLocationType.Unknown,
                
                "0" => GTFSStopLocationType.Stop,
                "1" => GTFSStopLocationType.Station,
                "2" => GTFSStopLocationType.EntranceExit,
                "3" => GTFSStopLocationType.GenericNode,
                "4" => GTFSStopLocationType.BoardingArea,
                
                _   => GTFSStopLocationType.Invalid
            };
        }
    }

        
    [Pure]
    public GTFSStop? GetStop(string id)
    {
        _stops.TryGetValue(id, out GTFSStop? stop);
        return stop;
    }

    public void CleanNames(Func<string, string> callback)
    {
        foreach (GTFSStop stop in _stops.Values)
            stop.CleanName(callback);
    }
}