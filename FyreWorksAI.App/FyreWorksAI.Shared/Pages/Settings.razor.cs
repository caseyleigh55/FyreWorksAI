using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace FyreWorksAI.Shared.Pages;

//******************************//
//******** Settings**************//
//******************************//
public partial class Settings
{

    private string StatusMessage { get; set; } = string.Empty;

    private LaborTemplate? DefaultTemplate =>
        Store.GetTemplate(Store.Workspace.Settings.DefaultTemplateId);

    protected override async Task OnInitializedAsync()
    {
        await Store.InitializeAsync();
    }

    private async Task SaveAsync()
    {
        await Store.SaveAsync();
        StatusMessage = "Settings saved.";
    }

    private async Task BackupAsync()
    {
        var path = await Store.CreateBackupAsync();
        StatusMessage = $"Backup created at {path}.";
    }

    private Task OpenDataDirectoryAsync() =>
        OpenStorageFolderAsync(Store.OpenDataDirectoryAsync, "data");

    private Task OpenAttachmentDirectoryAsync() =>
        OpenStorageFolderAsync(Store.OpenAttachmentDirectoryAsync, "attachments");

    private Task OpenExportDirectoryAsync() =>
        OpenStorageFolderAsync(Store.OpenExportDirectoryAsync, "exports");

    private Task OpenBackupDirectoryAsync() =>
        OpenStorageFolderAsync(Store.OpenBackupDirectoryAsync, "backups");

    private async Task OpenStorageFolderAsync(Func<Task> openAction, string label)
    {
        await openAction();
        StatusMessage = $"Opened the {label} folder.";
    }
}
