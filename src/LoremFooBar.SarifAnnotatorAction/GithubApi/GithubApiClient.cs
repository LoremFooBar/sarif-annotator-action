using System.Net.Http.Headers;
using DiffPatch;
using LoremFooBar.SarifAnnotatorAction.Model.Diff;
using LoremFooBar.SarifAnnotatorAction.Options;
using LoremFooBar.SarifAnnotatorAction.Utils;

namespace LoremFooBar.SarifAnnotatorAction.GithubApi;

public class GithubApiClient(EnvironmentInfo environmentInfo, AuthenticationOptions authOptions)
{
    private const string userAgent = "lfb-sarif-annotator";

    public async Task<Dictionary<string, AddedLinesInFile>> GetDiff(int pullRequestNumber)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://api.github.com");
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.diff"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authOptions.Token);
        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        var response = await httpClient.GetAsync(
            $"repos/{environmentInfo.RepoOwner}/{environmentInfo.RepoSlug}/pulls/{pullRequestNumber}");

        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new RequestFailedException(response, body);

        var fileDiffs = DiffParserHelper.Parse(body, Environment.NewLine);
        var diffDictionary = fileDiffs
            .Where(fd => !fd.Deleted)
            .Select(fd => new
            {
                fd.To,
                LineNumbers = fd.Chunks.SelectMany(chunk =>
                    chunk.Changes.Where(change => change.Add).Select(change => change.Index)).ToList(),
            })
            .GroupBy(x => x.To)
            .ToDictionary(x => x.Key, x =>
                new AddedLinesInFile(x.Key, x.SelectMany(y => y.LineNumbers).ToList()));

        return diffDictionary;
    }
}

public class RequestFailedException(HttpResponseMessage response, string? body)
    : Exception(
        $"Request to {response.RequestMessage?.RequestUri} " +
        $"failed with status {(int)response.StatusCode} {response.ReasonPhrase ?? ""}, response body: {body}");
