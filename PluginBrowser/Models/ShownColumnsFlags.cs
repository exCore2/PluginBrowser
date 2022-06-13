using System.Text.Json.Serialization;

namespace PluginBrowser.Models;

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShownColumnsFlags
{
    OriginalAuthor = 1 << 0,
    SelectedForkAuthor = 1 << 1,
    LatestCommitDate = 1 << 2,
    LatestCommitMessage = 1 << 3,
    LatestReleaseTitle = 1 << 4,
    LatestReleaseDate = 1 << 5,
    Description = 1 << 6,
}
