using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class UnknownParcelLockerAnalyzer : Analyzer
{
    public override string Name => "Unknown parcel lockers";

    public override string Description => "This report finds parcel lockers that do not appear to match to any known parcel locker brands.";

    public override AnalyzerGroup Group => AnalyzerGroups.Misc;


    public override List<Type> GetRequiredDataTypes() => new List<Type>()
    {
        typeof(OsmAnalysisData),
        typeof(ParcelLockerOperatorAnalysisData)
    };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;

        OsmDataExtract lockers = osmMasterData.Filter(
            new HasAnyValue("amenity", "parcel_locker")
        );

        // Prepare groups

        report.AddGroup(ReportGroup.Unknown, "Unknown parcel lockers");

        report.AddEntry(
            ReportGroup.Unknown,
            new DescriptionReportEntry(
                "These parcel lockers do not appear to match to any known parcel locker brands. These may be missing tags, have errors in the values or be from other brands."
            )
        );
        
        // Prepare known brands
        
        // Parse

        foreach (OsmElement element in lockers.Elements)
        {
            if (!MatchesKnownBrandedLocker(element))
            {
                report.AddEntry(
                    ReportGroup.Unknown,
                    new IssueReportEntry(
                        "Parcel locker " + element.OsmViewUrl + " doesn't seem to belong to a known brand.",
                        element.GetAverageCoord(),
                        MapPointStyle.Problem
                    )
                );
            }
        }
    }

    
    [Pure]
    private bool MatchesKnownBrandedLocker(OsmElement element)
    {
        // todo:
        return false;
    }


    private enum ReportGroup
    {
        Unknown,
    }
}