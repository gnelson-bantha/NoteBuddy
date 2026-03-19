using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using NoteBuddy.Components;
using NoteBuddy.Services;

// Configure the Blazor web application host
var builder = WebApplication.CreateBuilder(args);

// Bind to a fixed port so the tray app can reliably open the browser
builder.WebHost.UseUrls("http://localhost:5150");

// Register Blazor Server interactive rendering
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register MudBlazor UI component services
builder.Services.AddMudServices();

// Register the corkboard data service as a singleton for shared state across circuits
builder.Services.AddSingleton<CorkboardService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

// Serve uploaded pictures from %APPDATA%\NoteBuddy\uploads at the /uploads URL path
var uploadsPath = app.Services.GetRequiredService<CorkboardService>().GetUploadsPath();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// Serve static assets (CSS, JS, images) with fingerprinted URLs
app.MapStaticAssets();

// Map Razor components with interactive server-side rendering
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
