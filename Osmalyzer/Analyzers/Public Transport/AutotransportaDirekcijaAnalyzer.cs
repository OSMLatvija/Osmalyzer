using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class AutotransportaDirekcijaAnalyzer : PublicTransportAnalyzer<AutotransportaDirekcijaAnalysisData>
    {
        public override string Name => "Autotransporta Direkcija";

        public override string Description => "This checks the public transport route issues for " + Name;

        
        protected override string Label => "ATD";
    }
}