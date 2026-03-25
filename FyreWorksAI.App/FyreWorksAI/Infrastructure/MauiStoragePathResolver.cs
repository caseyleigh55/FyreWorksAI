using FyreWorksAI.Shared.Core.Services.Storage;

namespace FyreWorksAI.Infrastructure;

//******************************//
//***** Storage Path Root ******//
//******************************//

public sealed class MauiStoragePathResolver : IStoragePathResolver
{
    public string GetRootDirectory() =>
        Path.Combine(FileSystem.Current.AppDataDirectory, "FyreWorksAI");
}
