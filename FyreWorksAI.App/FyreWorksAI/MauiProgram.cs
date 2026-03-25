using FyreWorksAI.Infrastructure;
using FyreWorksAI.Shared.Core.DependencyInjection;
using FyreWorksAI.Shared.Core.Services.Attachments;
using FyreWorksAI.Shared.Core.Services.Storage;

namespace FyreWorksAI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<IStoragePathResolver, MauiStoragePathResolver>();
        builder.Services.AddSingleton<IAttachmentService, MauiAttachmentService>();
        builder.Services.AddSingleton<IWorkspaceLocationService, MauiWorkspaceLocationService>();
        builder.Services.AddFyreWorksCore();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        return builder.Build();
    }
}
