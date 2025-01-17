using LoremFooBar.SarifAnnotatorAction.ActionEnvironment;

namespace LoremFooBar.SarifAnnotatorAction.Options;

[Serializable]
public class ActionOptions
{
    /// <summary>
    /// Path to SARIF file. Can use patterns that are supported by <see cref="DirectoryInfo.GetFiles()" />
    /// </summary>
    public string SarifPathOrPattern { get; set; } = "";

    /// <summary>
    /// Whether to fail only for issues found in diff. For PRs - the PR diff, otherwise - diff with previous commit.
    /// </summary>
    public bool IncludeOnlyIssuesInDiff { get; set; } = true;

    /// <summary>
    /// Whether to fail current build step if any issues found.
    /// </summary>
    public bool FailWhenIssuesFound { get; set; }

    /// <summary>
    /// The pull request number to annotate
    /// </summary>
    public int PullRequestNumber { get; set; }

    public static ActionOptions FromEnvironment(IEnvironment environment) =>
        new()
        {
            FailWhenIssuesFound = environment.GetBool(EnvironmentVariable.FailWhenIssuesFound) ?? false,
            SarifPathOrPattern = environment.GetRequiredString(EnvironmentVariable.SarifFilePath),
            PullRequestNumber = int.Parse(environment.GetRequiredString(EnvironmentVariable.PullRequestNumber)),
        };
}
