﻿namespace Osmalyzer;

public class SEBLocationAnalyzer : BankLocationAnalyzer<SEBPointAnalysisData>
{
    protected override string BankName => "SEB";
}