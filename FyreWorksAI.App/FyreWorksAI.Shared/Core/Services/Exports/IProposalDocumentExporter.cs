namespace FyreWorksAI.Shared.Core.Services.Exports;

//******************************//
//**** Proposal Exporter *******//
//******************************//

public interface IProposalDocumentExporter
{
    Task<string> ExportAsync(
        string exportDirectoryPath,
        string documentBaseFileName,
        string htmlDocument,
        CancellationToken cancellationToken = default);
}
