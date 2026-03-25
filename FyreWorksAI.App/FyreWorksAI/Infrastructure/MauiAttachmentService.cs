using FyreWorksAI.Shared.Core.Services.Attachments;

namespace FyreWorksAI.Infrastructure;

//******************************//
//****** MAUI Attachments ******//
//******************************//

public sealed class MauiAttachmentService : IAttachmentService
{
    public bool SupportsPicking => true;
    public bool SupportsOpening => true;

    public async Task<IReadOnlyList<PickedFile>> PickFilesAsync()
    {
        try
        {
            var fileResults = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Select drawings, receipts, images, PDFs, Word, or Excel files"
            });

            if (fileResults is null)
            {
                return [];
            }

            var pickedFiles = new List<PickedFile>();
            foreach (var file in fileResults)
            {
                if (file is null || string.IsNullOrWhiteSpace(file.FullPath))
                {
                    continue;
                }

                pickedFiles.Add(new PickedFile(file.FullPath, file.FileName, file.ContentType ?? string.Empty));
            }

            return pickedFiles;
        }
        catch
        {
            return [];
        }
    }

    public async Task OpenAsync(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            return;
        }

        await Launcher.Default.OpenAsync(new OpenFileRequest("Open Attachment", new ReadOnlyFile(fullPath)));
    }
}
