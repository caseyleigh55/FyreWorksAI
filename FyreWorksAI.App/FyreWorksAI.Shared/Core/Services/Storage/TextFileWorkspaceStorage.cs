using System.Text.Json;

namespace FyreWorksAI.Shared.Core.Services.Storage;

//******************************//
//***** Text File Storage ******//
//******************************//

public sealed class TextFileWorkspaceStorage(IStoragePathResolver pathResolver) : IWorkspaceStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string DataFilePath => Path.Combine(pathResolver.GetRootDirectory(), "data", "fyreworks-data.txt");

    public async Task<FyreWorksWorkspace?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(DataFilePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(DataFilePath);
        return await JsonSerializer.DeserializeAsync<FyreWorksWorkspace>(stream, JsonOptions, cancellationToken);
    }

    public async Task SaveAsync(FyreWorksWorkspace workspace, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFilePath)!);
        await using var stream = File.Create(DataFilePath);
        await JsonSerializer.SerializeAsync(stream, workspace, JsonOptions, cancellationToken);
    }
}
