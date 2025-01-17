using LoremFooBar.SarifAnnotatorAction.ActionEnvironment;
using LoremFooBar.SarifAnnotatorAction.GithubApi;
using LoremFooBar.SarifAnnotatorAction.Model;
using LoremFooBar.SarifAnnotatorAction.Model.Diff;
using LoremFooBar.SarifAnnotatorAction.Options;
using LoremFooBar.SarifAnnotatorAction.Utils;
using Microsoft.CodeAnalysis.Sarif;
using Serilog;
using Serilog.Events;
using Result = Microsoft.CodeAnalysis.Sarif.Result;

namespace LoremFooBar.SarifAnnotatorAction;

public class ActionRunner
{
    private readonly ActionOptions _actionOptions;
    private readonly IEnvironment _environment;
    private readonly EnvironmentInfo _environmentInfo;
    private readonly GithubApiClient _githubApiClient;

    public ActionRunner()
    {
        _environment = new DefaultEnvironment();
        _environmentInfo = EnvironmentInfo.FromEnvironment(_environment);
        var authOptions = new AuthenticationOptions
        {
            Token = _environment.GetRequiredString(EnvironmentVariable.Token),
        };
        _actionOptions = ActionOptions.FromEnvironment(_environment);

        _githubApiClient = new GithubApiClient(_environmentInfo, authOptions);
    }

    public async Task Run()
    {
        var logLevel =
            _environment.GetBool(EnvironmentVariable.Debug) == true ? LogEventLevel.Debug : LogEventLevel.Warning;
        Console.WriteLine($"Log level is: {logLevel}");
        Log.Logger = new LoggerConfiguration().MinimumLevel.Is(logLevel).WriteTo.Console().CreateLogger();

        Log.Debug("Environment: {@EnvironmentInfo}", _environmentInfo);
        Log.Debug("Action options: {@ActionOptions}", _actionOptions);

        var file = GetSarifFile();

        SarifLog sarif;

        await using (var fileStream = file.OpenRead()) {
            sarif = SarifLog.Load(fileStream);
        }

        var results = await GetFilteredResultsByDiff(sarif);
        var annotations = new AnnotationsCreator(_environmentInfo).CreateAnnotationsFromSarifResults(results).ToList();

        Log.Debug("First 10 annotations created: {@Annotations}",
            annotations.Count > 10 ? annotations[..10] : annotations);

        WriteAnnotationsToConsole(annotations);

        if (_actionOptions.FailWhenIssuesFound && annotations is { Count: > 0 })
            throw new IssuesFoundException(annotations.Count);
    }

    private FileInfo GetSarifFile()
    {
        FileInfo file;

        if (Path.IsPathRooted(_actionOptions.SarifPathOrPattern))
            file = new FileInfo(_actionOptions.SarifPathOrPattern);
        else {
            var currentDir = new DirectoryInfo(Environment.CurrentDirectory);
            file =
                currentDir.GetFiles(_actionOptions.SarifPathOrPattern).FirstOrDefault()
                ?? throw new Exception(
                    $"No files found for {_actionOptions.SarifPathOrPattern} "
                    + $"relative to current dir {currentDir.FullName}"
                );
        }

        if (file is not { Exists: true })
            throw new FileNotFoundException(file.FullName);

        return file;
    }

    private async Task<IReadOnlyList<ResultWithRun>> GetFilteredResultsByDiff(SarifLog sarif)
    {
        var results = sarif.FlatResults();

        if (!_actionOptions.IncludeOnlyIssuesInDiff || results.Count == 0)
            return results;

        Log.Debug("filtering issues by changes in PR/commit. Total issues: {TotalIssues}", results.Count);

        var codeChanges = await _githubApiClient.GetDiff(_actionOptions.PullRequestNumber);
        var filteredIssues = results.Where(result => IsResultInChanges(result.Result, codeChanges)).ToList();

        Log.Debug("Total issues after filter: {TotalFilteredIssues}", filteredIssues.Count);

        return filteredIssues;

        static bool IsResultInChanges(Result result, IReadOnlyDictionary<string, AddedLinesInFile> codeChanges)
        {
            var physicalLocation = result.Locations.FirstOrDefault()?.PhysicalLocation;

            if (physicalLocation is not { ArtifactLocation: not null, Region: not null })
                return false;

            string file = physicalLocation.ArtifactLocation.Uri.OriginalString;
            int line = physicalLocation.Region.StartLine;

            return codeChanges.ContainsKey(file)
                   && codeChanges[file].LinesAdded.Any(addedLineNumber => line == addedLineNumber);
        }
    }

    private static void WriteAnnotationsToConsole(IEnumerable<Annotation> annotations)
    {
        var outputs = annotations.Select(a =>
            $"::error file={a.Path},line={a.Line},endLine={a.Line},title={a.Summary}::{a.Details}"
        );

        foreach (string output in outputs) {
            Console.WriteLine(output);
        }
    }
}

public class IssuesFoundException(int numberOfIssuesFound) : Exception($"Found {numberOfIssuesFound} issue(s)");
