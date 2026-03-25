using System.Diagnostics;
using FyreWorksAI.Shared.Core.Services.Storage;

namespace FyreWorksAI.Web.Infrastructure;

//******************************//
//*** Workspace Folder Open ****//
//******************************//

public sealed class WebWorkspaceLocationService : IWorkspaceLocationService
{
    public bool SupportsOpeningDirectories => true;

    public Task OpenDirectoryAsync(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return Task.CompletedTask;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }
}
