namespace Osmalyzer;

[UsedImplicitly]
public class RigasSatiksmeTicketVendingAnalyzer : Analyzer
{
    public override string Name => "Rigas Satiksme Ticket vending";



    public override string Description => "This report checks that all Rīgas satiksme ticket vending machines are found on the map.";

    public override AnalyzerGroup Group => AnalyzerGroup.PublicTransport;


    public override List<Type> GetRequiredDataTypes() =>
    [
        typeof(LatviaOsmAnalysisData),
        typeof(RigasSatiksmeVendingAnalysisData)
    ];


    public override void Run(IReadOnlyList<AnalysisData> datas, Report report)
    {
        // Load OSM data

        LatviaOsmAnalysisData osmData = datas.OfType<LatviaOsmAnalysisData>().First();

        OsmMasterData osmMasterData = osmData.MasterData;
        
        OsmDataExtract osmTicketVendingMachines = osmMasterData.Filter(
            new HasValue("amenity", "vending_machine"),
            new HasValue("vending", "public_transport_tickets")
        );
        // We assume only RS actualyl has vending machines. This may not be true in the future.
        
        List<TicketVendingMachine> listedTicketVendingMachines = datas.OfType<RigasSatiksmeVendingAnalysisData>().First().VendingMachines;
        
        Correlator<TicketVendingMachine> correlator = new Correlator<TicketVendingMachine>(
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

        CorrelatorReport correlatorReport = correlator.Parse(
            report,
            new MatchedPairBatch(),
            new UnmatchedItemBatch(),
            new MatchedFarPairBatch(),
            new UnmatchedOsmBatch()
        );
        
        
        // Tagging verification

        Validator<TicketVendingMachine> validator = new Validator<TicketVendingMachine>(
            correlatorReport,
            "Other ticket vending issues"
        );

        List<SuggestedAction> suggestedChanges = validator.Validate(
            report,
            true, // all elements we checked against are "real", so should follow the rules
            new ValidateElementHasValue("operator", "Rīgas satiksme"),
            new ValidateElementHasValue("operator:wikidata", "Q2280274"),
            new ValidateElementFixme()
        );

#if DEBUG
        SuggestedActionApplicator.ApplyAndProposeXml(osmMasterData, suggestedChanges, this);
#endif
    }
}