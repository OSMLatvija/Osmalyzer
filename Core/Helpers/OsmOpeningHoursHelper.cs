namespace Osmalyzer;

/// <summary>
/// Utilities for working with OSM opening hours strings.
/// </summary>
public static class OsmOpeningHoursHelper
{
    /// <summary>
    /// Merge sequential OSM syntax-based weekday entries with identical time parts into day ranges.
    /// Input lines are expected in forms like "Mo 08:00-12:00" or "Mo-Tu 08:00-12:00" or with seasonal prefix like "Sep-May Mo 08:00-12:00".
    /// Lines that include seasonal prefixes (e.g., "Sep-May ") are preserved as-is and not merged.
    /// The original input is not modified; a new list is returned.
    /// This does not assume valid input (returns as is), but does require valid input to actually process/merge.
    /// </summary>
    /// <param name="lines">Weekday opening hours entries.</param>
    /// <returns>New list with merged day ranges where applicable.</returns>
    [Pure]
    [MustUseReturnValue]
    public static List<string> MergeSequentialWeekdaysWithSameTimes(IEnumerable<string> lines)
    {
        List<string> merged = [ ];

        foreach (string line in lines)
        {
            if (merged.Count == 0)
            {
                // First line, just add it
                merged.Add(line);
                continue;
            }

            string previous = merged[^1];
            string current = line;

            // Skip special case month prefixes - they are their own line
            // e.g. "Sep-May Mo 08:00-12:00"
            if (current.Length > 3 && current[3] == '-')
            {
                merged.Add(current);
                continue;
            }

            if (DoesTimeMatch(previous, current) && AreDaysSequential(previous, current))
            {
                // Replace previous with the merged version
                merged[^1] = MergeDays(previous, current);
                continue;
            }

            merged.Add(current);
        }

        return merged;

        bool DoesTimeMatch(string a, string b)
        {
            int spaceIndex = a.IndexOf(' ');
            if (spaceIndex < 0)
                return false;

            // e.g. "Mo 08:00-12:00" => "08:00-12:00"
            // or with seasonal prefix: "Sep-May Mo 08:00-12:00" => "Mo 08:00-12:00" (won't match below)
            string aTime = a[(spaceIndex + 1) ..];

            // e.g. "Tu 08:00-12:00" => "08:00-12:00"
            // or "Mo-Tu 08:00-12:00" => "Tu 08:00-12:00" (won't match)
            if (b.Length < 4)
                return false;
            string bTime = b[3..];

            return aTime == bTime;
        }

        bool AreDaysSequential(string a, string b)
        {
            if (a.Length < 2 || b.Length < 2)
                return false;
            
            string aEndDay; // either the single day or the end day of a range, e.g. "Mo" or "Tu" in "Mo-Tu 08:00-12:00"
            
            // Is A range? 
            if (a.Length >= 3 && a[2] == '-')
            {
                if (a.Length >= 5)
                    aEndDay = a[3..5]; // e.g. "Mo-Tu 08:00-12:00" => "Tu"
                else                    
                    return false; // truncated, e.g. "Mo-T"
            }
            else
            {
                aEndDay = a[..2]; // e.g. "Mo 08:00-12:00" => "Mo"
            }

            string bDay = b[..2]; // always just the one day, e.g. "Tu 08:00-12:00" => "Tu"

            List<string> daysOfWeek = [ "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su" ];
            
            int aDayIndex = daysOfWeek.IndexOf(aEndDay);
            if (aDayIndex == -1)
                return false; // unrecognized day
            
            int bDayIndex = daysOfWeek.IndexOf(bDay);
            if (bDayIndex == -1)
                return false; // unrecognized day

            return aDayIndex == bDayIndex - 1;
        }

        string MergeDays(string a, string b)
        {
            // e.g. "Tu 08:00-12:00" => "08:00-12:00"
            string time = b[3..];

            // e.g. "Mo 08:00-12:00" => "Mo"
            // or "Mo-Tu 08:00-12:00" => "Mo"
            string aStartDay = a[..2];

            // e.g. "Tu 08:00-12:00" => "Tu"
            string bDay = b[..2];

            return aStartDay + "-" + bDay + " " + time;
        }
    }
}

