using System.Text;

namespace FyreWorksAI.Shared.Core.Services.Exports;

//******************************//
//***** HTML Export Writer *****//
//******************************//

public sealed class HtmlDocumentExporter : IHtmlDocumentExporter
{
    //******************************//
    //******** Export Flow *********//
    //******************************//

    public async Task<string> ExportAsync(
        string exportDirectoryPath,
        string documentBaseFileName,
        string htmlDocument,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(exportDirectoryPath);
        var outputPath = Path.Combine(exportDirectoryPath, $"{documentBaseFileName}.html");
        await File.WriteAllTextAsync(outputPath, htmlDocument, Encoding.UTF8, cancellationToken);
        return outputPath;
    }
}
