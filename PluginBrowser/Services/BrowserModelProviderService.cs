using System.Net.Http.Json;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Model;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace PluginBrowser.Services;

public class BrowserModelProviderService : IDisposable
{
    private readonly CompositeDisposable _disposable = new CompositeDisposable();
    private readonly HttpClient _httpClient;
    private readonly SettingsProviderService _settingsProviderService;

    public ReadOnlyReactiveProperty<Notification<BrowserModel?>> ModelLoadingState { get; }

    private static readonly List<PluginDescription> TestPluginList = new List<PluginDescription>
    {
        new PluginDescription("Test", "Him", new EquatableList<PluginForkDescription>
        {
            new PluginForkDescription("Him", "HisLocation", "Test",
                new CommitDescription("His", "123", "Him", DateTime.UtcNow.Subtract(TimeSpan.FromHours(2))),
                new EquatableList<ReleaseDescription> { new("release-1", "Release 1", new EquatableList<string>(), "Something", DateTime.UtcNow) }),
            new PluginForkDescription("Mine", "MyLocation", "Test",
                new CommitDescription("Mine", "456", "Me", DateTime.UtcNow.Subtract(TimeSpan.FromHours(1))),
                new EquatableList<ReleaseDescription> { new("release-2", "Release 2", new EquatableList<string>(), "Other", DateTime.UtcNow) }),
        }, "Desc1", "His"),
        new PluginDescription("Test 2", "Him", new EquatableList<PluginForkDescription>
        {
            new PluginForkDescription("Him", "HisLocation", "Test",
                new CommitDescription("His", "123", "Him", DateTime.UtcNow.Subtract(TimeSpan.FromHours(2))),
                new EquatableList<ReleaseDescription> { new("release-1", "Release 1", new EquatableList<string>(), "Something", DateTime.UtcNow - TimeSpan.FromHours(2)) }),
            new PluginForkDescription("Mine", "MyLocation", "Test",
                new CommitDescription("Mine", "456", "Me", DateTime.UtcNow.Subtract(TimeSpan.FromHours(1))),
                new EquatableList<ReleaseDescription>
                {
                    new("release-2", "Release 2", new EquatableList<string> { "file1", "file2" }, "Other", DateTime.UtcNow - TimeSpan.FromMinutes(1)),
                    new("release-3", "Release 3", new EquatableList<string> { "file1" }, "Other", DateTime.UtcNow),
                }),
        }, "Desc2", "His"),
        new PluginDescription("Test 3", "Him", new EquatableList<PluginForkDescription>
            {
                new PluginForkDescription("Him", "HisLocation", "Test",
                    new CommitDescription("His", "123", "Him", DateTime.UtcNow.Subtract(TimeSpan.FromHours(2))),
                    new EquatableList<ReleaseDescription>()),
                new PluginForkDescription("Mine", "MyLocation", "Test",
                    new CommitDescription("Mine", "456", "Me", DateTime.UtcNow.Subtract(TimeSpan.FromHours(1))),
                    new EquatableList<ReleaseDescription>()),
            },
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum",
            "His"),
    };

    private const string DefaultDataUri = "https://raw.githubusercontent.com/exCore2/PluginBrowserData/data/output.json";

    public BrowserModelProviderService(HttpClient httpClient, SettingsProviderService settingsProviderService)
    {
        _httpClient = httpClient;
        _settingsProviderService = settingsProviderService;
        ModelLoadingState = _settingsProviderService.Settings.CustomDataUrl
           .Select(async x =>
            {
                if (x == "test")
                {
                    return new BrowserModel(TestPluginList, DateTime.UtcNow, Constants.ExpectedModelVersion);
                }

                return await _httpClient.GetFromJsonAsync<BrowserModel>(x ?? DefaultDataUri) ??
                       throw new Exception("Returned model was null");
            })
           .Select(task => task
               .ToObservable()
               .StartWith((BrowserModel?)null)
               .Materialize()
               .Where(x => x.Kind != NotificationKind.OnCompleted))
           .Switch()
           .ToReadOnlyReactiveProperty<Notification<BrowserModel?>>()
           .AddTo(_disposable);
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }
}
