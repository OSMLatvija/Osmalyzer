using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class RigasSatiksmeAnalyzer : PublicTransportAnalyzer<RigasSatiksmeAnalysisData>
    {
        public override string Name => "Rigas Satiksme";

        public override string? Description => null;

        
        protected override string Label => "RS";
    }
}