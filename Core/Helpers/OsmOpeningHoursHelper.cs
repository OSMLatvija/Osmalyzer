using System.Linq;
using System.Text.RegularExpressions;

namespace Osmalyzer;

/// <summary>
/// Utilities for working with OSM opening hours strings.
/// </summary>
public static class OsmOpeningHoursHelper
{
    /// <summary>
    /// Merge sequential weekday entries with identical time parts into day ranges.
    /// Input lines are expected in forms like "Mo 08:00-12:00" or "Mo-Tu 08:00-12:00" or with seasonal prefix like "Sep-May Mo 08:00-12:00".
    /// Lines that include seasonal prefixes (e.g., "Sep-May ") are preserved as-is and not merged.
    /// The original input is not modified; a new list is returned.
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
                merged.Add(line);
                continue;
            }

            string previous = merged[merged.Count - 1];
            string current = line;

            // Skip special case month prefixes - they are their own line
            // e.g. "Sep-May Mo 08:00-12:00"
            if (current.Length > 3 && current[3] == '-')
            {
                merged.Add(current);
                continue;
            }

            if (TimeMatches(previous, current) && DaysSequential(previous, current))
            {
                // Replace previous with the merged version
                merged[merged.Count - 1] = MergeDays(previous, current);
                continue;
            }

            merged.Add(current);
        }

        return merged;

        bool TimeMatches(string a, string b)
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

        bool DaysSequential(string a, string b)
        {
            string daysRegex = @"\b(?:Mo|Tu|We|Th|Fr|Sa|Su)\b";
            List<string> daysOfWeek = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];
            string lastDayA = Regex.Matches(a, daysRegex).Last().Value;
            string firstDayB = Regex.Match(b, daysRegex).Value;
            return daysOfWeek.IndexOf(firstDayB) == daysOfWeek.IndexOf(lastDayA) + 1;
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

