namespace Osmalyzer;

public interface IDatedAnalysisData : ICachableAnalysisData
{
    bool DataDateHasDayGranularity { get; }

        
    DateTime RetrieveDataDate();
}