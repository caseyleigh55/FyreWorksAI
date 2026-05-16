using Microsoft.Extensions.DependencyInjection;

namespace FyreWorksAI.Shared.Core.DependencyInjection;

//******************************//
//****** Service Wiring ********//
//******************************//

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFyreWorksCore(this IServiceCollection services)
    {
        services.AddScoped<PageSectionNavigationState>();
        services.AddSingleton<IHtmlDocumentExporter, HtmlDocumentExporter>();
        services.AddSingleton<IWorkspaceStorage, TextFileWorkspaceStorage>();
        services.AddSingleton<WorkspaceStore>();
        return services;
    }
}
