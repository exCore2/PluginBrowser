namespace Model;

public static class GithubUrls
{
    public static string Release(string owner, string repository, string? release)
    {
        return $"https://github.com/{owner}/{repository}/releases/{release}";
    }

    public static string Repository(string owner, string repository)
    {
        return $"https://github.com/{owner}/{repository}";
    }
}
