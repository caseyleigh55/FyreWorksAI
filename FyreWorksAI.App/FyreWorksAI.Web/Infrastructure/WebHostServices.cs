using FyreWorksAI.Shared;

namespace FyreWorksAI.Web;

public sealed class WebStoragePathResolver(IWebHostEnvironment environment) : IStoragePathResolver
{
    public string GetRootDirectory() =>
        Path.Combine(environment.ContentRootPath, "App_Data");
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
