﻿namespace Osmalyzer;

[UsedImplicitly]
public class LiepajasTransportsAnalyzer : PublicTransportAnalyzer<LiepajasTransportsAnalysisData>
{
    public override string Name => "Liepajas Sabiedriskais Transports";


    protected override string Label => "LST";
    
    
    protected override void CleanUpGtfsData(GTFSNetwork gtfsNetwork)
    {
        // We don't have any known global issues
    }
}