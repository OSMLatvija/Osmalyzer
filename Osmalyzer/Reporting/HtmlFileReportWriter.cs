using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using JetBrains.Annotations;

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
        // TODO: StringBuilder
        
        string bodyContent = "<h2>Report for " + title + "</h2>" + Environment.NewLine;

        bodyContent += "<p>" + PolishLine(report.Description) + "</p>" + Environment.NewLine;

        List<ReportGroup> groups = report.CollectGroups();

        bool needTOC = groups.Count > 1 && groups.Sum(g => g.TotalEntryCount) > 30;

        if (needTOC)
        {
            bodyContent += "<h3>Sections</h3>" + Environment.NewLine;
            bodyContent += "<ul class=\"custom-list toc-list\">" + Environment.NewLine;
            for (int g = 0; g < groups.Count; g++)
            {
                int importantEntryCount = groups[g].ImportantEntryCount;
                
                bodyContent += @"<li><a href=""#g" + (g + 1) + @""">" + groups[g].Title + "</a>";

                if (importantEntryCount > 0)
                    if (groups[g].ShowImportantEntryCount)
                        bodyContent += @" (" + importantEntryCount + ")";

                bodyContent += @"</li>" + Environment.NewLine;
            }

            bodyContent += "</ul>" + Environment.NewLine;
        }
        
        if (report.NeedMap)
        {
            // Define (shared) leaflet icons
            
            bodyContent += @"<script>" + Environment.NewLine;

            foreach (LeafletIcon leafletIcon in EmbeddedIcons.Icons.OfType<LeafletIcon>())
                AddIcon(leafletIcon.Name, leafletIcon.Size);

            void AddIcon(string iconName, int size)
            {
                bodyContent += @"var " + iconName + " = L.icon({" + Environment.NewLine;
                bodyContent += @"iconUrl: 'icons/" + iconName + ".png'," + Environment.NewLine;
                bodyContent += @"iconSize: ["+size+", "+size+"]," + Environment.NewLine; // size of the icon
                bodyContent += @"iconAnchor: ["+(size/2)+", "+(size/2)+"]," + Environment.NewLine; // point of the icon which will correspond to marker's location
                bodyContent += @"popupAnchor: [2, -"+(size+2)+"]" + Environment.NewLine; // point from which the popup should open relative to the iconAnchor
                //bodyContent += @"shadowUrl: 'icons/leaf-shadow.png'," + Environment.NewLine;
                //bodyContent += @"shadowSize: [50, 64]," + Environment.NewLine; // size of the shadow
                //bodyContent += @"shadowAnchor: [4, 62]," + Environment.NewLine
                bodyContent += @"});" + Environment.NewLine;
            }

            bodyContent += @"</script>" + Environment.NewLine;
        }
            
        for (int g = 0; g < groups.Count; g++)
        {
            ReportGroup group = groups[g];

            if (needTOC)
                bodyContent += @"<h3 id=""g" + (g + 1) + @""">" + group.Title + "</h3>" + Environment.NewLine;
            else
                bodyContent += "<h3>" + group.Title + "</h3>" + Environment.NewLine;

            if (group.DescriptionEntry != null)
                bodyContent += "<p>" + PolishLine(group.DescriptionEntry.Text) + "</p>" + Environment.NewLine;

            if (!group.HaveAnyContentEntries)
                if (group.PlaceholderEntry != null)
                    bodyContent += "<p>" + PolishLine(group.PlaceholderEntry.Text) + "</p>" + Environment.NewLine;

            if (group.MapPointEntries.Count > 0)
                bodyContent += MakeMapContent(group.MapPointEntries, g);

            if (group.IssueEntryCount > 0)
            {
                bodyContent += "<ul class=\"custom-list issues-list\">" + Environment.NewLine;
                foreach (IssueReportEntry entry in group.CollectIssueEntries())
                    bodyContent += "<li>" + PolishLine(entry.Text) + "</li>" + Environment.NewLine;
                bodyContent += "</ul>" + Environment.NewLine;
            }

            if (group.GenericEntryCount > 0)
            {
                bodyContent += "<ul class=\"custom-list notes-list\">" + Environment.NewLine;
                foreach (GenericReportEntry entry in group.CollectGenericEntries())
                    bodyContent += "<li>" + PolishLine(entry.Text) + "</li>" + Environment.NewLine;
                bodyContent += "</ul>" + Environment.NewLine;
            }
        }

        bodyContent += "<h3>Source data</h3>" + Environment.NewLine;
        
        bodyContent += "<ul>" + Environment.NewLine;
        foreach (AnalysisData data in report.Datas)
            bodyContent += "<li>" + DataInfoLine(data) + "</li>" + Environment.NewLine;
        bodyContent += "</ul>" + Environment.NewLine;
        
        bodyContent += "Provided as is; mistakes possible." + Environment.NewLine;

        return bodyContent;

        
        [Pure]
        string MakeMapContent(IList<MapPointReportEntry> entries, int index)
        {
            string mapContent = GetMapTemplate();

            bool clustered = entries.Count > 100; // todo: always? per-report?

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

            string markerContent = MakeMarkersContent(entries, index);
            
            mapContent = ReplaceLocatorBlock(mapContent, "MARKERS", markerContent);
            
            mapContent = mapContent.Replace("_GI_", index.ToString());

            return mapContent;
        }

        [Pure]
        string MakeMarkersContent(IEnumerable<MapPointReportEntry> entries, int index)
        {
            string markersContent = ""; 
            
            foreach (MapPointReportEntry mapPointEntry in entries)
                markersContent += MakeMarkerContent(mapPointEntry);

            return markersContent;
            

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
                text += @" <a href=\""" + mapPointEntry.Coord.OsmUrl + @"\"" target=\""_blank\"" title=\""Open map at this location\"">🔗</a>";
                markerContent = markerContent.Replace("_TEXT_", text);

                return markerContent;

                
                [Pure]
                static string StyleMarkerGroup(LeafletIcon.IconGroup group)
                {
                    return group switch
                    {
                        LeafletIcon.IconGroup.Main => "mainMarkerGroup",
                        LeafletIcon.IconGroup.Sub  => "subMarkerGroup",

                        _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
                    };
                }

                [Pure]
                static string IconVisualColorGroup(ColorGroup group)
                {
                    return group switch
                    {
                        ColorGroup.Green => "green",
                        ColorGroup.Red   => "red",
                        ColorGroup.Other => "none",

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
            
        line = Regex.Replace(line, @"(https://www.openstreetmap.org/node/(\d+))", @"<a href=""$1"" class=""osm-link"">Node $2</a>");
        line = Regex.Replace(line, @"(https://www.openstreetmap.org/way/(\d+))", @"<a href=""$1"" class=""osm-link"">Way $2</a>");
        line = Regex.Replace(line, @"(https://www.openstreetmap.org/relation/(\d+))", @"<a href=""$1"" class=""osm-link"">Rel $2</a>");
        line = Regex.Replace(line, @"(https://www.openstreetmap.org/changeset/(\d+))", @"<a href=""$1"" class=""osm-link"">Changeset $2</a>");
        line = Regex.Replace(line, @"(https://www.openstreetmap.org/#map=\d{1,2}/(-?\d{1,3}\.\d+)/(-?\d{1,3}\.\d+))", @"<a href=""$1"" class=""osm-link"">Location $2, $3</a>");
            
        line = Regex.Replace(line, @"(https://overpass-turbo.eu/\?Q=[a-zA-Z0-9%\-_\.!*()+]+)", @"<a href=""$1"">Query</a>");

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
    private static string ReplaceLocatorBlock(string output, string locatorId, string value)
    {
        string locatorString = "<!--" + locatorId + "-->";

        int index = output.IndexOf(locatorString, StringComparison.Ordinal);
        
        return output[.. index] + value + output[(index + locatorString.Length) ..];
    }
}