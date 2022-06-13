using System.Reactive.Disposables;
using System.Reactive.Linq;
using Blazored.LocalStorage;
using PluginBrowser.Models;
using Reactive.Bindings.Extensions;

namespace PluginBrowser.Services;

public class SettingsProviderService : IDisposable
{
    private readonly SettingsStoreService _settingsStoreService;
    private readonly ISyncLocalStorageService _localStorageService;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public SettingsProviderService(SettingsStoreService settingsStoreService, ISyncLocalStorageService localStorageService)
    {
        _settingsStoreService = settingsStoreService;
        _localStorageService = localStorageService;
        Settings = GetSettings();
    }

    public Settings Settings { get; }

    private Settings GetSettings()
    {
        const string key = "pluginBrowserSettings";
        Settings settings;
        if (_settingsStoreService.Settings != null)
        {
            settings = _settingsStoreService.Settings;
        }
        else
        {
            try
            {
                settings = _localStorageService.GetItem<Settings>(key) ?? Settings.CreateDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to read settings, using default ones: {ex}");
                settings = Settings.CreateDefault();
            }

            _settingsStoreService.Settings = settings;
        }

        settings.ShownColumns.CombineLatest(settings.UseEndorsedForks, settings.CustomDataUrl).ToUnit().Subscribe(_ => _localStorageService.SetItem(key, settings)).AddTo(_disposables);
        return settings;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
