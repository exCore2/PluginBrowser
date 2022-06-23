using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Model;
using Octokit;

public class Methods
{
    public static async Task GenerateNewData()
    {
        await using var inputStream = File.Exists("input.txt") ? File.OpenRead("input.txt") : Console.OpenStandardInput();
        await using var outputStream = Console.OpenStandardOutput();
        var inputModel = JsonSerializer.Deserialize<InputModel>(inputStream);
        var secret = File.Exists("key.txt")
                         ? File.ReadAllText("key.txt")
                         : Environment.GetEnvironmentVariable("gh_token") ?? throw new Exception("Auth token not found");
        var githubApi = new GitHubClient(new ProductHeaderValue("test-plugin-browser"))
            { Credentials = new Credentials(secret) };
        var plugins = new List<PluginDescription>();
        foreach (var plugin in inputModel.Plugins)
        {
            try
            {
                var forks = new EquatableList<PluginForkDescription>();
                foreach (var pluginRepository in plugin.Repositories)
                {
                    try
                    {
                        var repo = await githubApi.Repository.Get(pluginRepository.Author, pluginRepository.Name);
                        var releases = await githubApi.Repository.Release.GetAll(repo.Id, new ApiOptions { PageCount = 1 });
                        var releaseDescriptions = releases
                           .Where(x => x.PublishedAt != null)
                           .Select(release => new ReleaseDescription(release.TagName, release.Name, release.Assets.Select(a => a.Name).ToEquatableList(), release.Body,
                                release.CreatedAt.UtcDateTime))
                           .ToEquatableList();

                        var defaultBranch = await githubApi.Repository.Branch.Get(repo.Id, repo.DefaultBranch);
                        var commit = await githubApi.Repository.Commit.Get(repo.Id, defaultBranch.Commit.Sha);
                        var commitDescription = new CommitDescription(commit.Commit.Message, commit.Commit.Sha, commit.Author.Login, commit.Commit.Committer.Date.UtcDateTime);
                        forks.Add(new PluginForkDescription(pluginRepository.Author, pluginRepository.Name, commitDescription, releaseDescriptions));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Unable to process fork {pluginRepository}: {ex}");
                    }
                }

                plugins.Add(new PluginDescription(plugin.Name, plugin.OriginalAuthor, forks, plugin.Description, plugin.EndorsedAuthor));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to process repository {plugin}: {ex}");
            }
        }

        var browserModel = new BrowserModel(plugins, DateTime.UtcNow);
        JsonSerializer.Serialize(outputStream, browserModel);
    }

    public static async Task PostUpdateNotes(FileInfo newFilePath, FileInfo oldFilePath, Uri releaseWebHook)
    {
        List<PluginForkDescription> oldContent;
        BrowserModel browserModel;
        await using (var stream1 = newFilePath.OpenRead())
            browserModel = JsonSerializer.Deserialize<BrowserModel>(stream1);
        var newContent = browserModel.PluginDescriptions.SelectMany(p => p.Forks.Select(f => f)).ToList();
        await using (var stream2 = oldFilePath.OpenRead())
            oldContent = JsonSerializer.Deserialize<BrowserModel>(stream2).PluginDescriptions.SelectMany(p => p.Forks.Select(f => f)).ToList();
        var forkToPluginMap = browserModel.PluginDescriptions.SelectMany(p => p.Forks.Select(f => (p, f))).ToDictionary(x => x.f, x => x.p);
        var changedPlugins = newContent.Join(oldContent, x => (x.Author, x.Name), x => (x.Author, x.Name), (a, b) => (a, b)).Where(x => !x.a.Equals(x.b)).ToList();
        var newCommits = changedPlugins.Where(x => !x.a.LatestCommit.Equals(x.b.LatestCommit)).Select(x => x.a).ToList();
        var newReleases = changedPlugins.Where(x => x.a.LatestRelease?.Equals(x.b.LatestRelease) != true)
           .Select(x => (x.a, x.a.Releases.ExceptBy(x.b.Releases.Select(r => r.Id), r => r.Id).ToList()))
           .ToList();
        var changedPluginDetails = newCommits
           .OuterJoinUnique(newReleases.AsNullable(), x => x, x => x?.a!)
           .Select(x => (x.Item1 ?? x.Item2?.a!, x.Item1?.LatestCommit, x.Item2?.Item2))
           .ToList();
        var newPlugins = newContent.ExceptBy(oldContent.Select(x => (x.Author, x.Name)), x => (x.Author, x.Name)).ToList();
        var sb = new StringBuilder();
        if (!changedPluginDetails.Any() && !newPlugins.Any())
        {
            Console.WriteLine("No updates, exiting");
            return;
        }
        
        sb.AppendLine("__**Warning! This message was formed automatically and contains content from third-party sources. Proceed with caution**__");
        foreach (var (key, changed, @new) in changedPluginDetails.GroupBy(x => forkToPluginMap[x.Item1])
                    .OuterJoinUnique(newPlugins.GroupBy(x => forkToPluginMap[x]), x => x.Key, x => x.Key).Select(x => (x.Item1?.Key ?? x.Item2?.Key!, x.Item1, x.Item2))
                    .OrderBy(x => x.Item1.Name))
        {
            sb.AppendLine($"\nPlugin __**{key.Name}**__");
            if (@new != null)
            {
                foreach (var fork in @new.OrderBy(x => x.Author).ThenBy(x => x.Name))
                {
                    sb.AppendLine($"**New** fork by __{fork.Author}__ (<{GithubUrls.Repository(fork.Author, fork.Name)}>)");
                    var processedCommitMessage = SubstringBefore(fork.LatestCommit.Message, new[] { '\r', '\n' }).Replace("`", "");
                    sb.AppendLine($"Latest commit at {fork.LatestCommit.Date.Format()} with message `{processedCommitMessage}`");
                    var latestRelease = fork.LatestRelease;
                    if (latestRelease != null)
                    {
                        var displayTitle = latestRelease.Title.Replace("`", "");
                        sb.AppendLine($"Latest release: `{displayTitle}` at {latestRelease.Date.Format()} (<{GithubUrls.Release(fork.Author, fork.Name, latestRelease.Id)}>)");
                    }
                }
            }

            if (changed != null)
            {
                foreach (var (fork, newCommit, newForkReleases) in changed.OrderBy(x => x.Item1.Author).ThenBy(x => x.Item1.Name))
                {
                    sb.AppendLine($"__{fork.Author}__'s fork (<{GithubUrls.Repository(fork.Author, fork.Name)}>)");
                    if (newCommit != null)
                    {
                        var processedCommitMessage = SubstringBefore(newCommit.Message, new[] { '\r', '\n' }).Replace("`", "");
                        sb.AppendLine($"New commits since last update: latest commit at {newCommit.Date.Format()} with message `{processedCommitMessage}`");
                    }

                    if (newForkReleases != null && newForkReleases.Any())
                    {
                        foreach (var release in newForkReleases.OrderBy(x => x.Date))
                        {
                            var displayTitle = release.Title.Replace("`", "");
                            sb.AppendLine($"New release: `{displayTitle}` at {release.Date.Format()} (<{GithubUrls.Release(fork.Author, fork.Name, release.Id)}>)");
                        }
                    }
                }
            }
        }

        var postContent = sb.ToString();
        Console.WriteLine($"Prepared message {postContent}");

        using (var httpClient = new HttpClient())
        {
            await httpClient.PostAsJsonAsync(releaseWebHook, new
            {
                content = postContent,
                embeds = new[]
                {
                    new
                    {
                        title = "Data generated using <https://instantsc.github.io/PluginBrowser>, check it out for the full plugin list", 
                        color = 1127128
                    }
                }
            });
        }
    }


    private static string SubstringBefore(string s, char[] symbols)
    {
        var firstIndex = s.IndexOfAny(symbols);
        if (firstIndex == -1) return s;
        return s.Substring(0, firstIndex);
    }
}
