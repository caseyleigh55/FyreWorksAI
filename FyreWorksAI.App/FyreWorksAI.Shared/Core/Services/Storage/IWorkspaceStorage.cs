namespace FyreWorksAI.Shared.Core.Services.Storage;

//******************************//
//***** Workspace Storage ******//
//******************************//

public interface IWorkspaceStorage
{
    string DataFilePath { get; }
    Task<FyreWorksWorkspace?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(FyreWorksWorkspace workspace, CancellationToken cancellationToken = default);
}
