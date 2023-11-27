using System.IO;

namespace Osmalyzer;

public abstract class ReportWriter
{
    /// <summary>
    /// If child report writers create files, then they should go into this subfolder.
    /// Relevant <see cref="Reporter"/> should create/clear it.
    /// </summary>
    private const string outputFolder = @"output";


    public static string OutputPath => Path.GetFullPath(outputFolder);

        
    public abstract void Save(Report report);
}