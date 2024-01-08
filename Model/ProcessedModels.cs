using System.Text.Json.Serialization;

namespace Model;

public static class Constants
{
    public const string ExpectedModelVersion = "1";
}

public record BrowserModel(List<PluginDescription> PluginDescriptions, DateTime Updated, string ModelVersion);

public record PluginDescription(string Name, string OriginalAuthor, EquatableList<PluginForkDescription> Forks, string Description, string? EndorsedAuthor);

public record PluginForkDescription(string Author, string Location, string Name, CommitDescription LatestCommit, EquatableList<ReleaseDescription> Releases)
{
    [JsonIgnore]
    public ReleaseDescription? LatestRelease => Releases.MaxBy(x => x.Date);
}

public record CommitDescription(string Message, string Hash, string Author, DateTime Date);

public record ReleaseDescription(string Id, string Title, EquatableList<string> FilesAttached, string Description, DateTime Date);