using FyreWorksAI.Shared;
using FyreWorksAI.Web;
using FyreWorksAI.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<IStoragePathResolver, WebStoragePathResolver>();
builder.Services.AddSingleton<IAttachmentService, UnsupportedAttachmentService>();
builder.Services.AddSingleton<IWorkspaceLocationService, WebWorkspaceLocationService>();
builder.Services.AddFyreWorksCore();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(FyreWorksAI.Shared._Imports).Assembly);

app.Run();
