using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Osmalyzer;

public static class ZipHelper
{
    /// <summary>
    /// From https://github.com/icsharpcode/SharpZipLib/wiki/Unpack-a-Zip-with-full-control-over-the-operation
    /// </summary>
    public static void ExtractZipFile(string archivePath, string outFolder)
    {
        using FileStream fileStream = File.OpenRead(archivePath);
            
        using ZipFile zipFile = new ZipFile(fileStream);
            
        foreach (ZipEntry zipEntry in zipFile)
        {
            if (!zipEntry.IsFile)
                continue; // Ignore directories

            string entryFileName = zipEntry.Name;

            string fullZipToPath = Path.Combine(outFolder, entryFileName);
            string directoryName = Path.GetDirectoryName(fullZipToPath)!;
            if (directoryName.Length > 0)
                Directory.CreateDirectory(directoryName);

            using Stream? zipStream = zipFile.GetInputStream(zipEntry);
            using Stream fsOutput = File.Create(fullZipToPath);
            StreamUtils.Copy(zipStream, fsOutput, new byte[4096]); // 4K is optimum
        }
    }
}