namespace Osmalyzer
{
    /// <summary>
    /// Shown always before the main entries (even if there are none)
    /// </summary>
    public class DescriptionReportEntry : ReportEntry
    {
        public DescriptionReportEntry(string text)
            : base(text)
        {
        }
    }
}