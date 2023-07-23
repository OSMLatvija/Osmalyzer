using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class BankLocationAnalyzer : Analyzer
    {
        public override string Name => "Bank Locations";

        public override string Description => "This report checks that all POIs from bank lists are mapped.";

        public override List<Type> GetRequiredDataTypes() => new List<Type>() { typeof(OsmAnalysisData), typeof(SwedbankPointAnalysisData) };


        public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
        {
            // Load OSM data

            OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

            OsmMasterData osmMasterData = osmData.MasterData;

            OsmDataExtract osmPoints = osmMasterData.Filter(
                new IsNode(),
                new HasAnyValue("amenity", "atm", "bank")
            );

            OsmDataExtract osmAtms = osmPoints.Filter(
                new IsNode(),
                new HasAnyValue("amenity", "atm")
            );

            OsmDataExtract osmBanks = osmPoints.Filter(
                new HasValue("amenity", "bank")
            );

            // Get Bank data

            List<BankPoint> points = datas.OfType<SwedbankPointAnalysisData>().First().Points;

            // Parse

            report.AddGroup(ReportGroup.Issues, "Issues", null, "All POIs appear to be mapped.");

            report.AddGroup(ReportGroup.Stats, "Matched POIs");

            const string bankName = "Swedbank";
            
            foreach (BankPoint bankPoint in points)
            {
                const double seekDistance = 100;

                
                OsmDataExtract relevantElements = bankPoint.Type switch
                {
                    BankPointType.Branch                  => osmBanks,
                    BankPointType.AtmWithdrawalAndDeposit => osmAtms,
                    BankPointType.AtmWithdrawal           => osmAtms,
                    _                                     => throw new NotImplementedException()
                };

                List<OsmNode> closestElements = relevantElements.GetClosestNodesTo(bankPoint.Coord, seekDistance);

                
                if (closestElements.Count == 0)
                {
                    report.AddEntry(
                        ReportGroup.Issues,
                        new IssueReportEntry(
                            "No OSM " + bankPoint.TypeString + " found in " + seekDistance + " m range of " +
                            bankName + " " + bankPoint.TypeString + " `" + bankPoint.Name + "` (`" + bankPoint.Address + "`) at " + bankPoint.Coord.OsmUrl,
                            new SortEntryAsc(SortOrder.NoPoint),
                            bankPoint.Coord
                        )
                    );
                }
                else
                {
                    OsmNode? matchedOsmPoint = closestElements.FirstOrDefault(t => DoesOsmPointMatchBankPoint(t, bankPoint, bankName));
            
                    if (matchedOsmPoint != null)
                    {
                        double matchedPointDistance = OsmGeoTools.DistanceBetween(matchedOsmPoint.coord, bankPoint.Coord);
            
                        if (matchedPointDistance > 30)
                        {
                            report.AddEntry(
                                ReportGroup.Issues,
                                new IssueReportEntry(
                                    "Matching OSM " + bankPoint.TypeString + " " + matchedOsmPoint.OsmViewUrl + " found close to " +
                                    bankName + " " + bankPoint.TypeString + " `" + bankPoint.Name + "` (`" + bankPoint.Address + "`), " +
                                    "but it's far away (" + matchedPointDistance.ToString("F0") + " m), expected at " + bankPoint.Coord.OsmUrl,
                                    new SortEntryAsc(SortOrder.PointFar),
                                    bankPoint.Coord
                                )
                            );
                        }
            
                        report.AddEntry(
                            ReportGroup.Stats,
                            new MapPointReportEntry(
                                matchedOsmPoint.coord,
                                "`" + bankPoint.Name + "` (`" + bankPoint.Address + "`) " +
                                "matched " + matchedOsmPoint.OsmViewUrl + " " +
                                "at " + matchedPointDistance.ToString("F0") + " m"
                            )
                        );
                        
                        // todo: denomination
                        // todo: species
                        // todo: start_date
                    }
                }
            }
        }

        [Pure]
        private static bool DoesOsmPointMatchBankPoint(OsmNode osmPoint, BankPoint bankPoint, string bankName)
        {
            // We are assuming the type was matched already
            
            string? osmName =
                osmPoint.GetValue("operator") ??
                osmPoint.GetValue("brand") ??
                osmPoint.GetValue("name") ??
                null; 
                
            return osmName != null && osmName.ToLower().Contains(bankName.ToLower());
        }


        private enum ReportGroup
        {
            Issues,
            Stats
        }

        private enum SortOrder // values used for sorting
        {
            NoPoint = 0,
            PointFar = 1
        }
    }
}