namespace FyreWorksAI.Shared.Core.Services.Exports;

//******************************//
//***** HTML Exporter **********//
//******************************//

public interface IHtmlDocumentExporter
{
    Task<string> ExportAsync(
        string exportDirectoryPath,
        string documentBaseFileName,
        string htmlDocument,
        CancellationToken cancellationToken = default);
}
