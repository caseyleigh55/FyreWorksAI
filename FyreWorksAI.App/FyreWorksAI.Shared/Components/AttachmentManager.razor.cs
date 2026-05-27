using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using FyreWorksAI.Shared.Core.Services.Status;

namespace FyreWorksAI.Shared.Components;

//******************************//
//******** AttachmentManager*****//
//******************************//
public partial class AttachmentManager
{
    [Parameter, EditorRequired]
    public required List<AttachmentRecord> Attachments { get; set; }

    [Parameter, EditorRequired]
    public required string Area { get; set; }

    [Parameter, EditorRequired]
    public required Guid OwnerId { get; set; }

    [Parameter]
    public string Eyebrow { get; set; } = "Documents";

    [Parameter]
    public string Title { get; set; } = "Attachments";

    [Parameter]
    public bool InitiallyExpanded { get; set; } = true;

    private string StatusMessage { get; set; } = string.Empty;
    private bool IsExpanded { get; set; }

    protected override void OnInitialized()
    {
        IsExpanded = InitiallyExpanded;
    }

    private void ToggleExpanded() =>
        IsExpanded = !IsExpanded;

    private async Task UploadAsync()
    {
        var addedCount = await Store.AddAttachmentsAsync(Attachments, Area, OwnerId);
        StatusMessage = StatusMessageFormatter.WithTimestamp(
            addedCount > 0
                ? $"{addedCount} attachment(s) added."
                : "No files were added.");

        if (addedCount > 0)
        {
            await Store.SaveAsync();
        }
    }

    private async Task OpenAsync(AttachmentRecord attachment)
    {
        StatusMessage = string.Empty;
        await Store.OpenAttachmentAsync(attachment);
    }

    private async Task RemoveAsync(AttachmentRecord attachment)
    {
        Store.RemoveAttachment(Attachments, attachment);
        await Store.SaveAsync();
        StatusMessage = StatusMessageFormatter.WithTimestamp("Attachment removed.");
    }
}
