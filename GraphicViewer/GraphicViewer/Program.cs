using GraphicViewer;
using GraphicViewer.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// SvgViewerService needs IJSRuntime — register as Scoped (not Singleton)
// because IJSRuntime is Scoped in Blazor WASM
builder.Services.AddScoped<SvgViewerService>();

await builder.Build().RunAsync();
