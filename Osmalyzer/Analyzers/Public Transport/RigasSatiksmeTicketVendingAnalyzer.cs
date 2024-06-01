using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmalyzer;

[UsedImplicitly]
public class RigasSatiksmeTicketVendingAnalyzer : Analyzer
{
    public override string Name => "Rigas Satiksme Ticket vending";



    public override string Description => "This report checks that all Rīgas Satiksme ticket vending machines are found on the map.";

    public override AnalyzerGroup Group => AnalyzerGroups.PublicTransport;


    public override List<Type> GetRequiredDataTypes() => new List<Type>()
    {
        typeof(OsmAnalysisData),
        typeof(RigasSatiksmeVendingAnalysisData)
    };


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        OsmAnalysisData osmData = datas.OfType<OsmAnalysisData>().First();

        OsmDataExtract osmTicketVendingMachines = osmData.MasterData.Filter(
            new HasValue("amenity", "vending_machine"),
            new HasValue("vending", "public_transport_tickets")
        );
        // We assume only RS actualyl has vending machines. This may not be true in the future.
        
        List<GenericData> listedTicketVendingMachines = datas.OfType<RigasSatiksmeVendingAnalysisData>().First().VendingMachines;
        
        Correlator<GenericData> dataComparer = new Correlator<GenericData>(
            osmTicketVendingMachines,
            listedTicketVendingMachines,
            new MatchDistanceParamater(75), // often data points to shop instead of kiosk
            new MatchFarDistanceParamater(150), // some are really far from where the data says they ought to be
            new MatchExtraDistanceParamater(MatchStrength.Strong, 500), // allow really far for exact matches
            new DataItemLabelsParamater("ticket vending machine", "ticket vending machines"),
            new OsmElementPreviewValue("name", false),
            new MatchCallbackParameter<DepositPoint>(GetMatchStrength)
        );

        [Pure]
        MatchStrength GetMatchStrength(DepositPoint point, OsmElement element)
        {
            if (FuzzyAddressMatcher.Matches(element, point.Address))
                return MatchStrength.Strong;

            return MatchStrength.Good;
        }

        // Parse and report primary matching and location correlation

        CorrelatorReport correlatorReport = dataComparer.Parse(
            report,
            new MatchedPairBatch(),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new UnmatchedOsmBatch()
        );
        
        
        // Tagging verification

        Validator<GenericData> validator = new Validator<GenericData>(
            correlatorReport,
            "Other ticket vending issues"
        );

        validator.Validate(
            report,
            new ValidateElementHasValue("operator", "Rīgas Satiksme"),
            new ValidateElementHasValue("operator:wikidata", "Q2280274"),
            new ValidateElementFixme()
        );
    }
}