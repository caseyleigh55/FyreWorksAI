using FyreWorksAI.Shared.Core.Services.Storage;

namespace FyreWorksAI.Web.Infrastructure;

//******************************//
//***** Storage Path Root ******//
//******************************//

public sealed class WebStoragePathResolver(IWebHostEnvironment environment) : IStoragePathResolver
{
    public string GetRootDirectory() =>
        Path.Combine(environment.ContentRootPath, "App_Data");
}
