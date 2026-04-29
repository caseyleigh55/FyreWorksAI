using System.Diagnostics;
using System.Text;
using FyreWorksAI.Shared.Core.Services.Exports;

namespace FyreWorksAI.Infrastructure;

//******************************//
//*** MAUI Proposal Exporter ***//
//******************************//

public sealed class MauiProposalDocumentExporter : IProposalDocumentExporter
{
    private static readonly string[] EdgeExecutablePaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe")
    ];

    //******************************//
    //******** Export Flow *********//
    //******************************//

    public async Task<string> ExportAsync(
        string exportDirectoryPath,
        string documentBaseFileName,
        string htmlDocument,
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return await ExportHtmlFallbackAsync(exportDirectoryPath, documentBaseFileName, htmlDocument, cancellationToken);
        }

        var edgeExecutablePath = ResolveEdgeExecutablePath();
        if (string.IsNullOrWhiteSpace(edgeExecutablePath))
        {
            return await ExportHtmlFallbackAsync(exportDirectoryPath, documentBaseFileName, htmlDocument, cancellationToken);
        }

        Directory.CreateDirectory(exportDirectoryPath);

        var pdfOutputPath = Path.Combine(exportDirectoryPath, $"{documentBaseFileName}.pdf");
        var temporaryExportDirectoryPath = Path.Combine(Path.GetTempPath(), "FyreWorksAI", "proposal-export", Guid.NewGuid().ToString("N"));
        var temporaryHtmlPath = Path.Combine(temporaryExportDirectoryPath, $"{documentBaseFileName}.html");
        var temporaryUserDataDirectoryPath = Path.Combine(temporaryExportDirectoryPath, "edge-profile");

        try
        {
            Directory.CreateDirectory(temporaryUserDataDirectoryPath);
            await File.WriteAllTextAsync(temporaryHtmlPath, htmlDocument, Encoding.UTF8, cancellationToken);

            var exportProcess = StartPdfExportProcess(edgeExecutablePath, temporaryUserDataDirectoryPath, temporaryHtmlPath, pdfOutputPath);
            if (exportProcess is null)
            {
                return await ExportHtmlFallbackAsync(exportDirectoryPath, documentBaseFileName, htmlDocument, cancellationToken);
            }

            await exportProcess.WaitForExitAsync(cancellationToken);
            var pdfCreated = await WaitForPdfOutputAsync(pdfOutputPath, cancellationToken);
            return pdfCreated
                ? pdfOutputPath
                : await ExportHtmlFallbackAsync(exportDirectoryPath, documentBaseFileName, htmlDocument, cancellationToken);
        }
        catch
        {
            return await ExportHtmlFallbackAsync(exportDirectoryPath, documentBaseFileName, htmlDocument, cancellationToken);
        }
        finally
        {
            TryDeleteFile(temporaryHtmlPath);
            TryDeleteDirectory(temporaryExportDirectoryPath);
        }
    }

    //******************************//
    //******** PDF Helpers *********//
    //******************************//

    private static Process? StartPdfExportProcess(
        string edgeExecutablePath,
        string userDataDirectoryPath,
        string htmlFilePath,
        string pdfOutputPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = edgeExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--disable-gpu");
        startInfo.ArgumentList.Add($"--user-data-dir={userDataDirectoryPath}");
        startInfo.ArgumentList.Add("--no-pdf-header-footer");
        startInfo.ArgumentList.Add($"--print-to-pdf={pdfOutputPath}");
        startInfo.ArgumentList.Add(new Uri(htmlFilePath).AbsoluteUri);

        return Process.Start(startInfo);
    }

    private static async Task<bool> WaitForPdfOutputAsync(string pdfOutputPath, CancellationToken cancellationToken)
    {
        const int maxAttempts = 80;
        const int delayMilliseconds = 250;
        long lastKnownLength = -1;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(pdfOutputPath))
            {
                try
                {
                    var fileInfo = new FileInfo(pdfOutputPath);
                    if (fileInfo.Length > 0 && fileInfo.Length == lastKnownLength)
                    {
                        return true;
                    }

                    lastKnownLength = fileInfo.Length;
                }
                catch
                {
                    // Keep polling while the file is still being written.
                }
            }

            await Task.Delay(delayMilliseconds, cancellationToken);
        }

        return File.Exists(pdfOutputPath);
    }

    //******************************//
    //******* Fallback Flow ********//
    //******************************//

    private static async Task<string> ExportHtmlFallbackAsync(
        string exportDirectoryPath,
        string documentBaseFileName,
        string htmlDocument,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(exportDirectoryPath);
        var htmlOutputPath = Path.Combine(exportDirectoryPath, $"{documentBaseFileName}.html");
        await File.WriteAllTextAsync(htmlOutputPath, htmlDocument, Encoding.UTF8, cancellationToken);
        return htmlOutputPath;
    }

    private static string? ResolveEdgeExecutablePath() =>
        EdgeExecutablePaths.FirstOrDefault(File.Exists);

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Temporary export cleanup should not interrupt the user-visible export result.
        }
    }

    private static void TryDeleteDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
        catch
        {
            // Temporary export cleanup should not interrupt the user-visible export result.
        }
    }
}
