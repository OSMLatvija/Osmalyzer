using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Osmalyzer;

[UsedImplicitly]
public class PostCodeAnalyzer : Analyzer
{
    public override string Name => "Post Codes";

    public override string Description => "This report analyzes post code use.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData) };

    
    private static OsmPolygon _latviaPolygon = null!;


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract postcodedElements = osmMasterData.Filter(
            new HasKey("addr:postcode")
        );
        
        _latviaPolygon = BoundaryHelper.GetLatviaPolygon(osmData.MasterData);

        const double distantElementThreshold = 50_000; // meters

        // Prepare groups

        report.AddGroup(ReportGroup.Regions, "Post code regions");

        report.AddEntry(
            ReportGroup.Regions,
            new DescriptionReportEntry(
                "Post code regions (average coord from all found addresses)."
            )
        );

        report.AddGroup(ReportGroup.InvalidCodes, "Invalid post codes");

        report.AddEntry(
            ReportGroup.InvalidCodes,
            new DescriptionReportEntry(
                "These do not match expected syntax for Latvia - `LV-####`."
            )
        );
        
        report.AddEntry(
            ReportGroup.InvalidCodes,
            new PlaceholderReportEntry(
                "All post codes appear to be valid."
            )
        );
        
        report.AddGroup(ReportGroup.DistantElements, "Distant elements");

        report.AddEntry(
            ReportGroup.DistantElements,
            new DescriptionReportEntry(
                "Elements that are too far away from the average coord of their post code region (> " + distantElementThreshold + " meters)."
            )
        );
        
        report.AddEntry(
            ReportGroup.DistantElements,
            new PlaceholderReportEntry(
                "No elements are too far away from their post code region."
            )
        );
        
        // Parse
        
        // Find unique post codes
        // Also find/filter invalid ones

        Dictionary<string, List<OsmElement>> sortedElements = new Dictionary<string, List<OsmElement>>();
        
        foreach (OsmElement postcodedElement in postcodedElements.Elements)
        {
            string postcode = postcodedElement.GetValue("addr:postcode")!;

            CodeValidation validation = ValidCodeSyntax(postcode, postcodedElement);

            if (validation != CodeValidation.Valid)
            {
                if (validation == CodeValidation.InvalidInLatvia)
                {
                    report.AddEntry(
                        ReportGroup.InvalidCodes,
                        new IssueReportEntry(
                            "Invalid post code `" + postcode + "` on " + postcodedElement.OsmViewUrl + ".",
                            postcodedElement.GetAverageCoord(),
                            MapPointStyle.Problem
                        )
                    );
                }

                continue; // Skip this one
            }

            // Add to dictionary
            
            if (!sortedElements.ContainsKey(postcode))
                sortedElements[postcode] = new List<OsmElement>() { postcodedElement };
            else
                sortedElements[postcode].Add(postcodedElement);
        }
        
        // Generic info about postcodes
        
        report.AddEntry(
            ReportGroup.Regions,
            new GenericReportEntry(
                "There are " + sortedElements.Count + " unique post codes."
            )
        );
        
        // Find the average coord of each unique postocode

        Dictionary<string, OsmCoord> averageCoords = new Dictionary<string, OsmCoord>();
        
        foreach ((string? postcode, List<OsmElement>? elements) in sortedElements)
        {
            OsmCoord averageCoord = new OsmCoord(
                elements.Average(e => e.GetAverageCoord().lat),
                elements.Average(e => e.GetAverageCoord().lon)
            );
            
            averageCoords.Add(postcode, averageCoord);

            report.AddEntry(
                ReportGroup.Regions,
                new GenericReportEntry(
                    "Post code `" + postcode + "` region with " + elements.Count + " addressable elements.",
                    averageCoord,
                    MapPointStyle.Okay
                )
            );
        }
        
        // Find elements that are very far from their regions
        
        foreach ((string postcode, List<OsmElement> elements) in sortedElements)
        {
            OsmCoord averageCoord = averageCoords[postcode];
            
            foreach (OsmElement element in elements)
            {
                double distance = OsmGeoTools.DistanceBetween(averageCoord, element.GetAverageCoord());
                
                if (distance > distantElementThreshold)
                {
                    report.AddEntry(
                        ReportGroup.DistantElements,
                        new IssueReportEntry(
                            "Element " + element.OsmViewUrl + " is too far away (" + distance + " meters) from the average coord of the post code region `" + postcode + "`.",
                            element.GetAverageCoord(),
                            MapPointStyle.Problem
                        )
                    );
                }
            }
        }
    }

    
    [Pure]
    private static CodeValidation ValidCodeSyntax(string postcode, OsmElement element)
    {
        // Must be "LV-####"
        if (Regex.IsMatch(postcode, @"^LV-\d{4}$"))
            return CodeValidation.Valid;
        
        // Could be not in Latvia (i.e. not addr:country=LV)
        string? addrCountry = element.GetValue("addr:country");
        
        if (addrCountry != null && addrCountry != "LV")
            return CodeValidation.NotInLatvia; // implicitly belongs to another country by address, so we don't care

        if (!_latviaPolygon.ContainsElement(element, OsmPolygon.RelationInclusionCheck.Fuzzy))
            return CodeValidation.NotInLatvia; // outside exact admin border, so most likely belongs to another country, so we don't care

        return CodeValidation.InvalidInLatvia; // we couldn't rule this out as valid or non-Latvia, so invalid
    }


    private enum CodeValidation
    {
        Valid,
        InvalidInLatvia,
        NotInLatvia
    }

        
    private enum ReportGroup
    {
        Regions,
        InvalidCodes,
        DistantElements
    }
}