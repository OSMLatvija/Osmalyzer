namespace Osmalyzer;

/// <summary>
/// An entry for a <see cref="Report"/>.
/// </summary>
public abstract class ReportEntry
{
    public string Text { get; protected set; }
            
    public ReportEntryContext? Context { get; }
            
    public ReportEntry? SubEntry { get; protected set; }


    protected ReportEntry(string text, ReportEntryContext? context = null)
    {
        Text = text;
        Context = context;
    }
}