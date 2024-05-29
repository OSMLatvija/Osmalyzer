using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class UnknownParcelLockerAnalyzer : Analyzer
{
    public override string Name => "Unknown parcel lockers";

    public override string Description => "This report finds parcel lockers that do not appear to match to any known parcel locker brands.";

    public override AnalyzerGroup Group => AnalyzerGroups.ParcelLocker;


    public override List<Type> GetRequiredDataTypes() => new List<Type>()
    {
        typeof(OsmAnalysisData),
        typeof(ParcelLockerOperatorAnalysisData)
    };
        

    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Get parcel locker operator used branding names
        
        ParcelLockerOperatorAnalysisData operatorData = datas.OfType<ParcelLockerOperatorAnalysisData>().First();

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
        
        // Parse

        foreach (OsmElement element in lockers.Elements)
        {
            if (!MatchesKnownBrandedLocker(element, operatorData, out List<string>? comparedValues))
            {
                report.AddEntry(
                    ReportGroup.Unknown,
                    new IssueReportEntry(
                        "Parcel locker " + element.OsmViewUrl + " doesn't seem to belong to a known brand" +
                        (comparedValues!.Count > 0 ? " (compared values: " + string.Join(", ", comparedValues.Select(v => "`" + v + "`")) + ")" : ""),
                        element.GetAverageCoord(),
                        MapPointStyle.Problem
                    )
                );
            }
        }
    }

    
    [Pure]
    private static bool MatchesKnownBrandedLocker(OsmElement element, ParcelLockerOperatorAnalysisData operatorData, out List<string>? comparedValues)
    {
        foreach ((string? _, List<string>? values) in operatorData.Branding)
        {
            if (LockerMatchesBrand(element, values))
            {
                comparedValues = null;
                return true;
            }
        }

        comparedValues = new List<string>();
        
        string? osmName = element.GetValue("name");
        string? osmOperator = element.GetValue("operator");
        string? osmBrand = element.GetValue("brand");
        
        if (osmName != null) comparedValues.Add(osmName);
        if (osmOperator != null) comparedValues.Add(osmOperator);
        if (osmBrand != null) comparedValues.Add(osmBrand);
        
        return false;
        

        static bool LockerMatchesBrand(OsmElement element, List<string> values)
        {
            // todo: use known brand data (file)

            string? osmName = element.GetValue("name");

            if (osmName != null && values.Exists(sn => osmName.ToLower().Contains(sn.ToLower())))
                return true;

            string? osmOperator = element.GetValue("operator");

            if (osmOperator != null && values.Exists(sn => osmOperator.ToLower().Contains(sn.ToLower())))
                return true;

            string? osmBrand = element.GetValue("brand");

            if (osmBrand != null && values.Exists(sn => osmBrand.ToLower().Contains(sn.ToLower())))
                return true;
            
            return false;
        }
    }


    private enum ReportGroup
    {
        Unknown,
    }
}