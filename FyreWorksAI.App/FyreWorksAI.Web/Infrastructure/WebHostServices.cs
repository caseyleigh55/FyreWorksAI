using System.Diagnostics;
using FyreWorksAI.Shared;

namespace FyreWorksAI.Web;

public sealed class WebStoragePathResolver(IWebHostEnvironment environment) : IStoragePathResolver
{
    public string GetRootDirectory() =>
        Path.Combine(environment.ContentRootPath, "App_Data");
}

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

public sealed class UnsupportedAttachmentService : IAttachmentService
{
    public bool SupportsPicking => false;
    public bool SupportsOpening => false;

    public Task<IReadOnlyList<PickedFile>> PickFilesAsync() =>
        Task.FromResult<IReadOnlyList<PickedFile>>([]);

    public Task OpenAsync(string fullPath) =>
        Task.CompletedTask;
}
