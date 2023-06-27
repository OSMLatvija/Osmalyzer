using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class LiepajasTransportsAnalyzer : PublicTransportAnalyzer<LiepajasTransportsAnalysisData>
    {
        public override string Name => "Liepajas Sabiedriskais Transports";

        public override string? Description => null;

        
        protected override string Label => "LST";
    }
}