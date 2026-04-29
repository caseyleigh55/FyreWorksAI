using FyreWorksAI.Shared.Core.Services.Attachments;

namespace FyreWorksAI.Web.Infrastructure;

//******************************//
//**** Unsupported Files *******//
//******************************//

public sealed class UnsupportedAttachmentService : IAttachmentService
{
    public bool SupportsPicking => false;
    public bool SupportsOpening => false;

    public Task<IReadOnlyList<PickedFile>> PickFilesAsync(string pickerTitle = "Select files") =>
        Task.FromResult<IReadOnlyList<PickedFile>>([]);

    public Task OpenAsync(string fullPath) =>
        Task.CompletedTask;
}
