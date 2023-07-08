using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class RezeknesSatiksmeAnalyzer : PublicTransportAnalyzer<RezeknesSatiksmeAnalysisData>
    {
        public override string Name => "Rezeknes Satiksme";

        public override string Description => "This checks the public transport route issues for " + Name;

        
        protected override string Label => "RS";
    }
}