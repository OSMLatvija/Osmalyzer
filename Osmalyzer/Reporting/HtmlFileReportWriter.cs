using System;
using System.Collections.Generic;
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
            output = StripSection(output, "MAP");

        string title = HttpUtility.HtmlEncode(report.Name);
        
        output = ReplaceMarker(output, "TITLE", title);
        output = ReplaceMarker(output, "DESCR_TITLE", title);

        string bodyContent = BuildContent(report, title);
        
        output = ReplaceMarker(output, "BODY", bodyContent);

        // Write
        
        ReportFileName = report.Name + " report.html";
            
        string fullReportFileName = outputFolder + "/" + ReportFileName;
            
        File.WriteAllText(fullReportFileName, output);
    }

    
    [Pure]
    private static string BuildContent(Report report, string title)
    {
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
                bodyContent += @"<li><a href=""#g" + (g + 1) + @""">" + groups[g].Title + "</a>" + (importantEntryCount > 0 ? @" (" + importantEntryCount + ")" : "") + @"</li>" + Environment.NewLine;
            }

            bodyContent += "</ul>" + Environment.NewLine;
        }
        
        if (report.NeedMap)
        {
            // Define (shared) leaflet icons
            
            bodyContent += @"<script>" + Environment.NewLine;

            AddIcon("greenCheckmarkIcon", "green_checkmark.png");
            AddIcon("orangeCheckmarkIcon", "orange_checkmark.png");
            AddIcon("redCrossIcon", "red_cross.png");

            void AddIcon(string iconVarName, string iconFileName)
            {
                bodyContent += @"var " + iconVarName + " = L.icon({" + Environment.NewLine;
                bodyContent += @"iconUrl: 'icons/" + iconFileName + "'," + Environment.NewLine;
                //bodyContent += @"shadowUrl: 'icons/leaf-shadow.png'," + Environment.NewLine;
                bodyContent += @"iconSize:     [16, 16], // size of the icon" + Environment.NewLine;
                //bodyContent += @"shadowSize:   [50, 64], // size of the shadow" + Environment.NewLine;
                bodyContent += @"iconAnchor:   [8, 8], // point of the icon which will correspond to marker's location" + Environment.NewLine;
                //bodyContent += @"shadowAnchor: [4, 62],  // the same for the shadow" + Environment.NewLine;
                bodyContent += @"popupAnchor:  [2, -10] // point from which the popup should open relative to the iconAnchor" + Environment.NewLine;
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

            if (group.MapPointEntries.Count > 0)
            {
                bodyContent += $@"<div class=""map"" id=""map{g}"" style=""width: 800px; height: 400px;""></div>" + Environment.NewLine;

                bodyContent += @"<script>" + Environment.NewLine;

                bodyContent += @$"var map = L.map('map{g}').setView([56.906, 24.505], 7);" + Environment.NewLine;
                bodyContent += @"L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {" + Environment.NewLine;
                bodyContent += @"    maxZoom: 21," + Environment.NewLine;
                bodyContent += @"    attribution: '&copy; <a href=""https://www.openstreetmap.org/copyright"">OSM</a>'" + Environment.NewLine;
                bodyContent += @"}).addTo(map);" + Environment.NewLine;
                bodyContent += @"var markerGroup = L.featureGroup().addTo(map);" + Environment.NewLine;
                
                foreach (MapPointReportEntry mapPointEntry in group.MapPointEntries)
                {
                    string lat = mapPointEntry.Coord.lat.ToString("F6");
                    string lon = mapPointEntry.Coord.lon.ToString("F6");
                    string text = PolishLine(mapPointEntry.Text).Replace("\"", "\\\"");
                    string mapUrl = @"<a href=\""" + mapPointEntry.Coord.OsmUrl + @"\"" target=\""_blank\"" title=\""Open map at this location\"">🔗</a>";
                    bodyContent += $@"L.marker([{lat}, {lon}], {{icon: greenCheckmarkIcon}}).addTo(markerGroup).bindPopup(""{text} {mapUrl}"");" + Environment.NewLine;
                }

                bodyContent += @"map.fitBounds(markerGroup.getBounds(), { maxZoom: 12, animate: false });" + Environment.NewLine;

                bodyContent += @"</script>" + Environment.NewLine;
            }
        }

        bodyContent += "<br>Data as of " + HttpUtility.HtmlEncode(report.DataDates) + ". Provided as is; mistakes possible." + Environment.NewLine;

        return bodyContent;
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
    private static string GetOutputTemplate()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        
        const string resourcePath = @"Osmalyzer.Reporting.report template.html";

        using Stream stream = assembly.GetManifestResourceStream(resourcePath)!;
        
        using StreamReader reader = new StreamReader(stream);
        
        return reader.ReadToEnd();
    }

    [Pure]
    private static string StripSection(string output, string marker)
    {
        string fromString = "<!--" + marker + "-->";
        string toString = "<!--END " + marker + "-->";

        int startIndex = output.IndexOf(fromString, StringComparison.Ordinal);
        int endIndex = output.IndexOf(toString, startIndex, StringComparison.Ordinal);
        
        return output[.. startIndex] + output[(endIndex + toString.Length) ..];
    }

    [Pure]
    private static string ReplaceMarker(string output, string marker, string value)
    {
        string markerString = "<!--" + marker + "-->";

        int index = output.IndexOf(markerString, StringComparison.Ordinal);
        
        return output[.. index] + value + output[(index + markerString.Length) ..];
    }
}