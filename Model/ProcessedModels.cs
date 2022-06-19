using System.Text.Json.Serialization;

namespace Model;

public record BrowserModel(List<PluginDescription> PluginDescriptions, DateTime Updated);

public record PluginDescription(string Name, string OriginalAuthor, EquatableList<PluginForkDescription> Forks, string Description, string? EndorsedAuthor);

public record PluginForkDescription(string Author, string Name, CommitDescription LatestCommit, EquatableList<ReleaseDescription> Releases)
{
    [JsonIgnore]
    public ReleaseDescription? LatestRelease => Releases.MaxBy(x => x.Date);
}

public record CommitDescription(string Message, string Hash, string Author, DateTime Date);

public record ReleaseDescription(string Id, string Title, EquatableList<string> FilesAttached, string Description, DateTime Date);