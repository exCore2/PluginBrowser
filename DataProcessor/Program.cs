using System.Text.Json;
using Model;
using Octokit;

using var inputStream = File.Exists("input.txt") ? File.OpenRead("input.txt") : Console.OpenStandardInput();
using var outputStream = Console.OpenStandardOutput();
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
        var forks = new List<PluginForkDescription>();
        foreach (var pluginRepository in plugin.Repositories)
        {
            try
            {
                var repo = await githubApi.Repository.Get(pluginRepository.Author, pluginRepository.Name);
                var releases = await githubApi.Repository.Release.GetAll(repo.Id, new ApiOptions { PageCount = 1 });
                var releaseDescriptions = releases
                   .Where(x => x.PublishedAt != null)
                   .Select(release => new ReleaseDescription(release.TagName, release.Name, release.Assets.Select(a => a.Name).ToList(), release.Body,
                        release.CreatedAt.UtcDateTime))
                   .ToList();

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

record InputModel(List<PluginInfo> Plugins);

record PluginInfo(string Name, string OriginalAuthor, List<RepositoryInfo> Repositories, string Description, string? EndorsedAuthor);

record RepositoryInfo(string Author, string Name);
