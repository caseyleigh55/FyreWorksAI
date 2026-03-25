namespace FyreWorksAI.Shared.Core.Services.Storage;

//******************************//
//*** Workspace Folder Access **//
//******************************//

public interface IWorkspaceLocationService
{
    bool SupportsOpeningDirectories { get; }
    Task OpenDirectoryAsync(string fullPath);
}
