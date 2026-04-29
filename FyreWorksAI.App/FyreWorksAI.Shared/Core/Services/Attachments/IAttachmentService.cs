namespace FyreWorksAI.Shared.Core.Services.Attachments;

//******************************//
//****** Attachment Access *****//
//******************************//

public interface IAttachmentService
{
    bool SupportsPicking { get; }
    bool SupportsOpening { get; }
    Task<IReadOnlyList<PickedFile>> PickFilesAsync(string pickerTitle = "Select files");
    Task OpenAsync(string fullPath);
}
