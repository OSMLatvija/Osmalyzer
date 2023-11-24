using System;

namespace Osmalyzer;

public interface ICachableAnalysisData
{
    bool DataDateHasDayGranularity { get; }

        
    DateTime RetrieveDataDate();
}