using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using WalkMapFrontend;
using WalkMapFrontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WalkService>();

// FIX: Was hardcoded to "http://localhost:5195" — breaks in Azure.
//      Now reads ApiBaseUrl from wwwroot/appsettings.json which can be
//      swapped per environment without rebuilding the app.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? "http://localhost:5195";

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

await builder.Build().RunAsync();
