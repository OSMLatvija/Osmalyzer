namespace Osmalyzer
{
    public abstract class ReportEntry
    {
        public string Text { get; }
            
        public object? Context { get; }
            
        public ReportEntry? SubEntry { get; protected set; }


        protected ReportEntry(string text, object? context = null)
        {
            Text = text;
            Context = context;
        }
    }
}