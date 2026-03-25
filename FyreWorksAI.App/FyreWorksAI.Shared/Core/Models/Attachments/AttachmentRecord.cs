namespace FyreWorksAI.Shared.Core.Models.Attachments;

//******************************//
//******** Attachments *********//
//******************************//

public sealed class AttachmentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedOn { get; set; } = DateTime.Now;
}
