using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class RezeknesSatiksmeAnalyzer : PublicTransportAnalyzer<RezeknesSatiksmeAnalysisData>
    {
        public override string Name => "Rezeknes Satiksme";

        public override string? Description => null;

        
        protected override string Label => "RS";
    }
}