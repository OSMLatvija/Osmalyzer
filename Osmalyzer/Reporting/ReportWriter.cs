using System.Collections.Generic;

namespace Osmalyzer
{
    public abstract class ReportWriter
    {
        public abstract void Save(Report report, string name, List<AnalysisData> datas);
    }
}