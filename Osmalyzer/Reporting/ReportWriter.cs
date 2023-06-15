namespace Osmalyzer
{
    public abstract class ReportWriter
    {
        /// <summary>
        /// If child report writers create files, then they should go into this subfolder.
        /// Relevant <see cref="Reporter"/> should create/clear it.
        /// </summary>
        public const string outputFolder = @"output";

        
        public abstract void Save(Report report);
    }
}