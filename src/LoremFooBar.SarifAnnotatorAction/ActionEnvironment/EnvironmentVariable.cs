namespace LoremFooBar.SarifAnnotatorAction.ActionEnvironment;

public record struct EnvironmentVariable
{
    public readonly string Name;

    private EnvironmentVariable(string name) => Name = name;

    // action options
    public static EnvironmentVariable Debug { get; } = new("DEBUG");
    public static EnvironmentVariable FailWhenIssuesFound { get; } = new("FAIL_WHEN_ISSUES_FOUND");
    public static EnvironmentVariable SarifFilePath { get; } = new("SARIF_FILE_PATH");
    public static EnvironmentVariable PullRequestNumber { get; } = new("PULL_REQUEST_NUMBER");

    // auth options
    public static EnvironmentVariable Token { get; } = new("GH_TOKEN");

    // gh environment
    public static EnvironmentVariable CommitSha { get; } = new("GITHUB_SHA");
    public static EnvironmentVariable RepositoryName { get; } = new("GITHUB_REPOSITORY");
    public static EnvironmentVariable RepositoryOwner { get; } = new("GITHUB_REPOSITORY_OWNER");

    public static EnvironmentVariable CloneDir { get; } = new("GITHUB_WORKSPACE");
}
