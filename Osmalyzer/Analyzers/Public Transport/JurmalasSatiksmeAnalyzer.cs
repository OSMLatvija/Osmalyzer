using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class JurmalasSatiksmeAnalyzer : PublicTransportAnalyzer<JurmalasSatiksmeAnalysisData>
    {
        public override string Name => "Jurmalas Autobusu Satiksme";

        public override string Description => "This checks the public transport route issues for " + Name;

        
        protected override string Label => "JS";
    }
}