using System.Globalization;
using FyreWorksAI.Shared.Core.Services.Status;

namespace FyreWorksAI.Shared.Pages;

//******************************//
//******** Settings**************//
//******************************//
public partial class Settings
{

    private string StatusMessage { get; set; } = string.Empty;
    private string ProposalLogoPreviewDataUri { get; set; } = string.Empty;

    private LaborTemplate? DefaultTemplate =>
        Store.GetTemplate(Store.Workspace.Settings.DefaultTemplateId);

    private bool HasProposalLogo =>
        !string.IsNullOrWhiteSpace(Store.Workspace.Settings.ProposalLogoRelativePath);

    protected override async Task OnInitializedAsync()
    {
        await Store.InitializeAsync();
        RefreshProposalLogoPreview();
    }

    private async Task SaveAsync()
    {
        await Store.SaveAsync();
        StatusMessage = StatusMessageFormatter.WithTimestamp("Settings saved.");
    }

    private async Task BackupAsync()
    {
        var path = await Store.CreateBackupAsync();
        StatusMessage = StatusMessageFormatter.WithTimestamp($"Backup created at {path}.");
    }

    private Task OpenDataDirectoryAsync() =>
        OpenStorageFolderAsync(Store.OpenDataDirectoryAsync, "data");

    private Task OpenAttachmentDirectoryAsync() =>
        OpenStorageFolderAsync(Store.OpenAttachmentDirectoryAsync, "attachments");

    private Task OpenBrandingDirectoryAsync() =>
        OpenStorageFolderAsync(Store.OpenBrandingDirectoryAsync, "branding");

    private Task OpenExportDirectoryAsync() =>
        OpenStorageFolderAsync(Store.OpenExportDirectoryAsync, "exports");

    private Task OpenBackupDirectoryAsync() =>
        OpenStorageFolderAsync(Store.OpenBackupDirectoryAsync, "backups");

    private async Task OpenStorageFolderAsync(Func<Task> openAction, string label)
    {
        await openAction();
        StatusMessage = StatusMessageFormatter.WithTimestamp($"Opened the {label} folder.");
    }

    private async Task SelectProposalLogoAsync()
    {
        var path = await Store.SaveProposalLogoAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage = StatusMessageFormatter.WithTimestamp("No supported image logo was selected.");
            return;
        }

        await Store.SaveAsync();
        RefreshProposalLogoPreview();
        StatusMessage = StatusMessageFormatter.WithTimestamp($"Proposal logo saved at {path}.");
    }

    private async Task RemoveProposalLogoAsync()
    {
        Store.RemoveProposalLogo();
        await Store.SaveAsync();
        RefreshProposalLogoPreview();
        StatusMessage = StatusMessageFormatter.WithTimestamp("Proposal logo removed.");
    }

    private void RefreshProposalLogoPreview()
    {
        ProposalLogoPreviewDataUri = Store.GetProposalLogoDataUri() ?? string.Empty;
    }
}
