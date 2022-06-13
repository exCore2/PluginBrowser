using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PluginBrowser;
using PluginBrowser.Services;
using PluginBrowser.Utils;

var builder = WebAssemblyHostBuilder.CreateDefault();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped(_ => new HttpClient());
builder.Services.AddScoped<SettingsProviderService>();
builder.Services.AddScoped<BrowserModelProviderService>();
builder.Services.AddSingleton<SettingsStoreService>();
builder.Services.AddBlazoredLocalStorage(o => { o.JsonSerializerOptions.Converters.Add(new ReactivePropertyConverterFactory()); });

await builder.Build().RunAsync();
