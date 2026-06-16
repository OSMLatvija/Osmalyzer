using System.IO.Compression;
using System.Reflection;

namespace Osmalyzer;

/// <summary>
/// Handles packaging and copying raw data files to the output directory for debugging purposes.
/// Only includes files that are below a certain size threshold.
/// </summary>
/// <remarks>Slop</remarks>
public static class DataFilePackager
{
    /// <summary>
    /// Maximum size in bytes for a single data file to be included in the deployed package
    /// </summary>
    public const long MaxDataFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    
    /// <summary>
    /// Maximum total size in bytes for all zipped data files to be included
    /// </summary>
    public const long MaxZippedDataSizeBytes = MaxDataFileSizeBytes; // same for now
    
    private const string DataOutputSubfolder = "data-files";
    
    
    /// <summary>
    /// Packages all data files once and returns a mapping from each AnalysisData to its packaged files.
    /// This is more efficient than packaging per-report since multiple reports can share the same data.
    /// </summary>
    public static Dictionary<AnalysisData, List<string>> PackageAllDataFiles(IEnumerable<AnalysisData> allDatas, string outputPath)
    {
        string dataOutputPath = Path.Combine(outputPath, DataOutputSubfolder);
        
        // Create data files directory if it doesn't exist
        if (!Directory.Exists(dataOutputPath))
            Directory.CreateDirectory(dataOutputPath);
        
        Dictionary<AnalysisData, List<string>> dataToFilesMapping = new Dictionary<AnalysisData, List<string>>();
        
        foreach (AnalysisData data in allDatas)
        {
            List<string> dataFiles = FindDataFiles(data);
            List<string> packagedFiles = [ ];
            
            if (dataFiles.Count == 0)
            {
                dataToFilesMapping[data] = packagedFiles;
                continue;
            }
            
            if (dataFiles.Count == 1)
            {
                // Single file - copy directly if under threshold
                string sourceFile = dataFiles[0];
                FileInfo fileInfo = new FileInfo(sourceFile);
                
                if (fileInfo.Length <= MaxDataFileSizeBytes)
                {
                    string fileName = GetSafeFileName(data, Path.GetFileName(sourceFile));
                    string destPath = Path.Combine(dataOutputPath, fileName);
                    
                    File.Copy(sourceFile, destPath, true);
                    packagedFiles.Add(Path.Combine(DataOutputSubfolder, fileName));
                    
                    Console.WriteLine($"  Packaged data file for {data.Name}: {fileName} ({FormatFileSize(fileInfo.Length)})");
                }
                else
                {
                    Console.WriteLine($"  Skipped large data file for {data.Name}: {FormatFileSize(fileInfo.Length)}");
                }
            }
            else
            {
                // Multiple files - create a zip if total size is under threshold
                string zipFileName = GetSafeFileName(data, data.GetType().Name + "-data.zip");
                string zipPath = Path.Combine(dataOutputPath, zipFileName);
                
                // Create temporary zip to check size
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (string dataFile in dataFiles)
                        {
                            string entryName = Path.GetFileName(dataFile);
                            archive.CreateEntryFromFile(dataFile, entryName, CompressionLevel.Optimal);
                        }
                    }
                    
                    long zipSize = memoryStream.Length;
                    
                    if (zipSize <= MaxZippedDataSizeBytes)
                    {
                        File.WriteAllBytes(zipPath, memoryStream.ToArray());
                        packagedFiles.Add(Path.Combine(DataOutputSubfolder, zipFileName));
                        
                        Console.WriteLine($"  Packaged {dataFiles.Count} data files for {data.Name} as: {zipFileName} ({FormatFileSize(zipSize)})");
                    }
                    else
                    {
                        Console.WriteLine($"  Skipped large zipped data for {data.Name}: {FormatFileSize(zipSize)}");
                    }
                }
            }
            
            dataToFilesMapping[data] = packagedFiles;
        }
        
        return dataToFilesMapping;
    }
    
    
    /// <summary>
    /// Finds all data files associated with an AnalysisData instance
    /// </summary>
    private static List<string> FindDataFiles(AnalysisData data)
    {
        List<string> files = [ ];
        
        // Use reflection to find properties that might contain file paths
        PropertyInfo[] properties = data.GetType().GetProperties(BindingFlags.Public | 
                                                                 BindingFlags.NonPublic | 
                                                                 BindingFlags.Instance);
        
        foreach (PropertyInfo prop in properties)
        {
            if (prop.Name.Contains("FileName") || prop.Name.Contains("FilePath"))
            {
                try
                {
                    object? value = prop.GetValue(data);
                    if (value is string filePath && File.Exists(filePath))
                    {
                        files.Add(filePath);
                    }
                }
                catch
                {
                    // Ignore properties that can't be accessed
                }
            }
        }
        
        // Also check for files matching the DataFileIdentifier pattern in the cache directory
        try
        {
            PropertyInfo? identifierProp = data.GetType().GetProperty("DataFileIdentifier", 
                                                                      BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (identifierProp != null)
            {
                object? identifierValue = identifierProp.GetValue(data);
                if (identifierValue is string identifier)
                {
                    string pattern = identifier + "*";
                    string cacheDir = AnalysisData.CacheBasePath;
                    
                    if (Directory.Exists(cacheDir))
                    {
                        List<string> matchingFiles = Directory.GetFiles(cacheDir, pattern, SearchOption.TopDirectoryOnly)
                                                              .Where(f => !f.EndsWith("-cache-date.txt")) // Exclude metadata files
                                                              .ToList();
                        
                        files.AddRange(matchingFiles);
                        
                        // Also check for subdirectory if it exists (for multi-file data like CulturalMonuments)
                        string subDir = Path.Combine(cacheDir, identifier);
                        if (Directory.Exists(subDir))
                        {
                            files.AddRange(Directory.GetFiles(subDir, "*", SearchOption.AllDirectories));
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore if we can't access the identifier
        }
        
        return files.Distinct().ToList();
    }
    
    
    /// <summary>
    /// Creates a safe file name based on the data name and original file name
    /// </summary>
    private static string GetSafeFileName(AnalysisData data, string originalFileName)
    {
        // Create a safe prefix from the data name
        string safePrefix = string.Join("_", data.Name.Split(Path.GetInvalidFileNameChars()));
        safePrefix = safePrefix.Replace(" ", "_");
        
        string extension = Path.GetExtension(originalFileName);
        string baseName = Path.GetFileNameWithoutExtension(originalFileName);
        
        // If the base name already contains the identifier, use it as-is
        if (baseName.Length < 50)
            return originalFileName;
        
        // Otherwise create a shorter name
        return $"{safePrefix}{extension}";
    }
    
    
    /// <summary>
    /// Formats a file size in a human-readable format
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = [ "B", "KB", "MB", "GB" ];
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
}


