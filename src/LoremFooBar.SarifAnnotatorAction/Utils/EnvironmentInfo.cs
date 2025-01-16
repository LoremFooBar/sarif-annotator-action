using LoremFooBar.SarifAnnotatorAction.ActionEnvironment;

namespace LoremFooBar.SarifAnnotatorAction.Utils;

public class EnvironmentInfo
{
    public required string RepoOwner { get; init; }

    public required string RepoSlug { get; init; }

    public required string CommitSha { get; init; }

    public required string CloneDir { get; init; }

    public static EnvironmentInfo FromEnvironment(IEnvironment environment)
    {
        string repositoryName = environment.GetRequiredString(EnvironmentVariable.RepositoryName);

        return new EnvironmentInfo
        {
            CommitSha = environment.GetRequiredString(EnvironmentVariable.CommitSha),
            RepoOwner = environment.GetRequiredString(EnvironmentVariable.RepositoryOwner),
            RepoSlug = repositoryName.Split('/')[^1],
            CloneDir = environment.GetRequiredString(EnvironmentVariable.CloneDir),
        };
    }
}
