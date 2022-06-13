using Reactive.Bindings;

namespace PluginBrowser.Models;

public class Settings
{
    public static Settings CreateDefault() => new();

    public ReactiveProperty<bool> UseEndorsedForks { get; init; } = new ReactiveProperty<bool>(true);

    public ReactiveProperty<ShownColumnsFlags> ShownColumns { get; init; } = new ReactiveProperty<ShownColumnsFlags>(
        ShownColumnsFlags.Description |
        ShownColumnsFlags.LatestCommitDate |
        ShownColumnsFlags.LatestCommitMessage |
        ShownColumnsFlags.LatestReleaseDate |
        ShownColumnsFlags.LatestReleaseTitle |
        ShownColumnsFlags.OriginalAuthor |
        ShownColumnsFlags.SelectedForkAuthor);

    public ReactiveProperty<string?> CustomDataUrl { get; init; } = new ReactiveProperty<string?>();
}
