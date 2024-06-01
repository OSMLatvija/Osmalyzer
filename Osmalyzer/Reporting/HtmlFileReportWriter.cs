using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Osmalyzer;

public class HtmlFileReportWriter : ReportWriter
{
    public string ReportFileName { get; private set; } = null!;


    public override void Save(Report report)
    {
        string output = GetOutputTemplate();

        if (!report.NeedMap)
            output = StripLocatorBlock(output, "MAP");

        string title = HttpUtility.HtmlEncode(report.Name);
        
        output = ReplaceLocatorBlock(output, "TITLE", title);
        output = ReplaceLocatorBlock(output, "DESCR_TITLE", title);

        string bodyContent = BuildContent(report, title);
        
        output = ReplaceLocatorBlock(output, "BODY", bodyContent);

        // Write
        
        ReportFileName = report.Name + " report.html";
            
        string fullReportFileName = Path.Combine(OutputPath, ReportFileName);
            
        File.WriteAllText(fullReportFileName, output);
    }

    
    [Pure]
    private static string BuildContent(Report report, string title)
    {
        StringBuilder str = new StringBuilder(); 
        
        str.AppendLine("<h2>Report for " + title + "</h2>");

        str.AppendLine("<p>" + PolishLine(report.Description) + "</p>");

        List<ReportGroup> groups = report.CollectGroups();

        bool needTOC = groups.Count > 1 && groups.Sum(g => g.TotalEntryCount) > 30;

        if (needTOC)
        {
            str.AppendLine("<h3>Sections</h3>");
            str.AppendLine("<ul class=\"custom-list toc-list\">");
            for (int g = 0; g < groups.Count; g++)
            {
                int importantEntryCount = groups[g].ImportantEntryCount;
                
                str.Append(@"<li><a href=""#g" + (g + 1) + @""">" + groups[g].Title + "</a>");

                if (importantEntryCount > 0)
                    if (groups[g].ShowImportantEntryCount)
                        str.Append(@" (" + importantEntryCount + ")");

                str.AppendLine(@"</li>");
            }

            str.AppendLine("</ul>");
        }
        
        if (report.NeedMap)
        {
            // Define (shared) leaflet icons
            
            str.AppendLine(@"<script>");

            foreach (LeafletIcon leafletIcon in EmbeddedIcons.Icons.OfType<LeafletIcon>())
                AddIcon(leafletIcon.Name, leafletIcon.Size);

            void AddIcon(string iconName, int size)
            {
                // todo: to template
                
                str.AppendLine(@"var " + iconName + " = L.icon({");
                str.AppendLine(@"iconUrl: 'icons/" + iconName + ".png',");
                str.AppendLine(@"iconSize: ["+size+", "+size+"],"); // size of the icon
                str.AppendLine(@"iconAnchor: ["+(size/2)+", "+(size/2)+"],"); // point of the icon which will correspond to marker's location
                str.AppendLine(@"popupAnchor: [2, -"+(size+2)+"]"); // point from which the popup should open relative to the iconAnchor
                //stringBuilder.AppendLine(@"shadowUrl: 'icons/leaf-shadow.png',");
                //stringBuilder.AppendLine(@"shadowSize: [50, 64],"); // size of the shadow
                //stringBuilder.AppendLine(@"shadowAnchor: [4, 62]," + Environment.NewLine
                str.AppendLine(@"});");
            }

            str.AppendLine(@"</script>");
        }
            
        for (int g = 0; g < groups.Count; g++)
        {
            ReportGroup group = groups[g];

            if (needTOC)
                str.AppendLine(@"<h3 id=""g" + (g + 1) + @""">" + group.Title + "</h3>");
            else
                str.AppendLine("<h3>" + group.Title + "</h3>");

            if (group.DescriptionEntry != null)
                str.AppendLine("<p>" + PolishLine(group.DescriptionEntry.Text) + "</p>");

            if (!group.HaveAnyContentEntries)
                if (group.PlaceholderEntry != null)
                    str.AppendLine("<p>" + PolishLine(group.PlaceholderEntry.Text) + "</p>");

            if (group.MapPointEntries.Count > 0)
                str.AppendLine(MakeMapContent(group.MapPointEntries, g));

            if (group.IssueEntryCount > 0)
            {
                str.AppendLine("<ul class=\"custom-list issues-list\">");
                foreach (IssueReportEntry entry in group.CollectIssueEntries())
                    str.AppendLine("<li>" + PolishLine(entry.Text) + "</li>");
                str.AppendLine("</ul>");
            }

            if (group.GenericEntryCount > 0)
            {
                str.AppendLine("<ul class=\"custom-list notes-list\">");
                foreach (GenericReportEntry entry in group.CollectGenericEntries())
                    str.AppendLine("<li>" + PolishLine(entry.Text) + "</li>");
                str.AppendLine("</ul>");
            }
        }

        str.AppendLine("<h3>Source data</h3>");
        
        str.AppendLine("<ul>");
        foreach (AnalysisData data in report.Datas)
            str.AppendLine("<li>" + DataInfoLine(data) + "</li>");
        str.AppendLine("</ul>");
        
        str.AppendLine("Provided as is; mistakes possible.");

        return str.ToString();

        
        [Pure]
        string MakeMapContent(IList<MapPointReportEntry> entries, int index)
        {
            string mapContent = GetMapTemplate();

            bool clustered = true; // entries.Count > 100; // todo: per-report? - it works really well, so not sure when not to use

            if (clustered)
            {
                mapContent = StripLocatorBlock(mapContent, "UNCLUSTERED");
                mapContent = StripLocators(mapContent, "CLUSTERED");
            }
            else
            {
                mapContent = StripLocatorBlock(mapContent, "CLUSTERED");
                mapContent = StripLocators(mapContent, "UNCLUSTERED");
            }

            string markerContent = MakeMarkersContent(entries);
            
            mapContent = ReplaceLocatorBlock(mapContent, "MARKERS", markerContent);
            
            mapContent = mapContent.Replace("_GI_", index.ToString());

            return mapContent;
        }

        [Pure]
        string MakeMarkersContent(IEnumerable<MapPointReportEntry> entries)
        {
            StringBuilder markerStr = new StringBuilder(); 
            
            foreach (MapPointReportEntry mapPointEntry in entries)
                markerStr.Append(MakeMarkerContent(mapPointEntry));

            return markerStr.ToString();
            

            string MakeMarkerContent(MapPointReportEntry mapPointEntry)
            {
                string markerContent = GetMarkerTemplate(); 

                string lat = mapPointEntry.Coord.lat.ToString("F6");
                string lon = mapPointEntry.Coord.lon.ToString("F6");
                markerContent = markerContent.Replace("_LAT_", lat);
                markerContent = markerContent.Replace("_LON_", lon);

                LeafletIcon icon = EmbeddedIcons.Icons.OfType<LeafletIcon>().First(i => i.Styles.Contains(mapPointEntry.Style));

                string markerGroup = StyleMarkerGroup(icon.Group);
                markerContent = markerContent.Replace("_FEATURES_", markerGroup);
                
                string iconName = icon.Name;
                markerContent = markerContent.Replace("_ICON_", iconName);

                string iconGroup = IconVisualColorGroup(icon.ColorGroup);
                markerContent = markerContent.Replace("_GROUP_", iconGroup);

                string text = PolishLine(mapPointEntry.Text).Replace("\"", "\\\"");

                // Add map location link, if there wasn't one explicitly already
                string coordUrl = mapPointEntry.Coord.OsmUrl;
                if (!text.Contains(coordUrl))
                    text += @" <a href=\""" + coordUrl + @"\"" target=\""_blank\"" title=\""Open map at this location\"">🔗</a>";

                markerContent = markerContent.Replace("_TEXT_", text);

                return markerContent;

                
                [Pure]
                static string StyleMarkerGroup(LeafletIcon.IconGroup group)
                {
                    // todo: dehardcode somehow?
                    
                    return group switch
                    {
                        LeafletIcon.IconGroup.Main => "mg",
                        LeafletIcon.IconGroup.Sub  => "sg",

                        _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
                    };
                }

                [Pure]
                static string IconVisualColorGroup(ColorGroup group)
                {
                    return group switch
                    {
                        ColorGroup.Green  => "green",
                        ColorGroup.Orange => "orange",
                        ColorGroup.Red    => "red",
                        ColorGroup.Other  => "none",

                        _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
                    };
                }
            }
        }
    }

    [Pure]
    private static string PolishLine(string line)
    {
        line = HttpUtility.HtmlEncode(line);
            
        // URLs
        
        line = Regex.Replace(line, @"(https://osm.org/node/(\d+))", @"<a href=""$1"" class=""osm-link"" target=""_blank"">Node $2</a>");
        line = Regex.Replace(line, @"(https://osm.org/way/(\d+))", @"<a href=""$1"" class=""osm-link"" target=""_blank"">Way $2</a>");
        line = Regex.Replace(line, @"(https://osm.org/relation/(\d+))", @"<a href=""$1"" class=""osm-link"" target=""_blank"">Rel $2</a>");
        line = Regex.Replace(line, @"(https://osm.org/changeset/(\d+))", @"<a href=""$1"" class=""osm-link"" target=""_blank"">Changeset $2</a>");
        line = Regex.Replace(line, @"(https://osm.org/#map=\d{1,2}/(-?\d{1,3}\.\d+)/(-?\d{1,3}\.\d+))", @"<a href=""$1"" class=""osm-link"" target=""_blank"">$2, $3</a>");
            
        line = Regex.Replace(line, @"(https://overpass-turbo.eu/\?Q=[a-zA-Z0-9%\-_\.!*()+]+)", @"<a href=""$1"" target=""_blank"">Query</a>");
        
        line = Regex.Replace(line, @"(https://www.wikidata.org/entity/Q(\d+))", @"<a href=""$1"" class=""osm-link"" target=""_blank"">Q$2</a>");

        line = Regex.Replace(line, @"(https://mantojums.lv/(\d+))", @"<a href=""$1"" target=""_blank"">#$2</a>");

        // Other syntax
        line = Regex.Replace(line, @"```([^`]+)```", @"<pre class=""osm-tag""><code>$1</code></pre>");

        line = Regex.Replace(line, @"`([^`]+)`", @"<code class=""osm-tag"">$1</code>");

        line = line.Replace(Environment.NewLine, "<br>");
            
        return line;
    }

    [Pure]
    private static string DataInfoLine(AnalysisData data)
    {
        string s = HttpUtility.HtmlEncode(data.Name);

        if (data is IDatedAnalysisData datedData)
            s += " as of " +
                 (datedData.DataDateHasDayGranularity ?
                     data.DataDate!.Value.ToString("yyyy-MM-dd HH:mm:ss") :
                     data.DataDate!.Value.ToString("yyyy-MM-dd"));
        else
            s += " (undated)";

        if (data.ReportWebLink != null)
            s += @" <a href=""" + data.ReportWebLink + @""" target=\""_blank\"">Link</a>";

        return s;
    }

    [Pure]
    private static string GetOutputTemplate()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        
        const string resourcePath = @"Osmalyzer.Reporting.Report_templates.main.html";

        using Stream stream = assembly.GetManifestResourceStream(resourcePath)!;
        
        using StreamReader reader = new StreamReader(stream);
        
        return reader.ReadToEnd();
    }

    [Pure]
    private static string GetMapTemplate()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        
        const string resourcePath = @"Osmalyzer.Reporting.Report_templates.map.html";

        using Stream stream = assembly.GetManifestResourceStream(resourcePath)!;
        
        using StreamReader reader = new StreamReader(stream);
        
        return reader.ReadToEnd();
    }

    [Pure]
    private static string GetMarkerTemplate()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        
        const string resourcePath = @"Osmalyzer.Reporting.Report_templates.marker.js";

        using Stream stream = assembly.GetManifestResourceStream(resourcePath)!;
        
        using StreamReader reader = new StreamReader(stream);
        
        return reader.ReadToEnd();
    }

    [Pure]
    private static string StripLocatorBlock(string output, string locatorId)
    {
        string fromString = "<!--" + locatorId + "-->";
        string toString = "<!--END " + locatorId + "-->";

        int startIndex = output.IndexOf(fromString, StringComparison.Ordinal);
        int endIndex = output.IndexOf(toString, startIndex, StringComparison.Ordinal);
        
        return output[.. startIndex] + output[(endIndex + toString.Length) ..];
    }

    [Pure]
    private static string StripLocators(string output, string locatorId)
    {
        string fromString = "<!--" + locatorId + "-->" + Environment.NewLine;
        string toString = "<!--END " + locatorId + "-->" + Environment.NewLine;
        
        return output.Replace(fromString, "").Replace(toString, "");
    }

    
    [Pure]
    public static string ReplaceLocatorBlock(string output, string locatorId, string value)
    {
        string locatorString = "<!--" + locatorId + "-->";

        int index = output.IndexOf(locatorString, StringComparison.Ordinal);
        
        return output[.. index] + value + output[(index + locatorString.Length) ..];
    }
}