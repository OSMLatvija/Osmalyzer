using System;
using System.Collections.Generic;

namespace Osmalyzer
{
    public abstract class Analyzer
    {
        public abstract string Name { get; }


        public abstract List<Type> GetRequiredDataTypes();

        public abstract void Run(IEnumerable<AnalysisData> datas);
    }
}