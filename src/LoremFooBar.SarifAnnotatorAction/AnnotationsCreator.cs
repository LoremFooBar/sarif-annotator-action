using LoremFooBar.SarifAnnotatorAction.Model;
using LoremFooBar.SarifAnnotatorAction.Utils;
using Microsoft.CodeAnalysis.Sarif;
using Serilog;

namespace LoremFooBar.SarifAnnotatorAction;

public class AnnotationsCreator
{
    private readonly Uri? _cloneDirUri;

    public AnnotationsCreator(EnvironmentInfo environmentInfo)
    {
        bool endsWithSlash = environmentInfo.CloneDir.EndsWith('\\') || environmentInfo.CloneDir.EndsWith('/');
        string cloneDir;

        if (endsWithSlash)
            cloneDir = environmentInfo.CloneDir;
        else {
            cloneDir = environmentInfo.CloneDir.StartsWith('/')
                ? environmentInfo.CloneDir + '/'
                : environmentInfo.CloneDir + '\\';
        }

        Uri.TryCreate("file://" + cloneDir, UriKind.Absolute, out _cloneDirUri);
        Log.Debug("Clone dir uri {Uri}", _cloneDirUri);
    }

    public IEnumerable<Annotation> CreateAnnotationsFromSarifResults(IReadOnlyList<ResultWithRun> results)
    {
        if (results.Count == 0) yield break;

        foreach (var (run, result) in results) {
            var physicalLocation = result.Locations.FirstOrDefault()?.PhysicalLocation;

            if (physicalLocation is null) continue;

            string filePath = physicalLocation.ArtifactLocation.Uri.OriginalString;

            if (string.IsNullOrEmpty(filePath)) continue;

            var rule = result.GetRule(run);
            string details = result.Message.Text + (rule.HelpUri == null ? "" : "\n" + rule.HelpUri);
            string pathRelativeToCloneDir = GetPathRelativeToCloneDir(physicalLocation.ArtifactLocation, run);

            Annotation? annotation;

            try {
                annotation = new Annotation
                {
                    Path = pathRelativeToCloneDir,
                    Line = physicalLocation.Region.StartLine,
                    Summary = string.IsNullOrWhiteSpace(rule.ShortDescription?.Text)
                        ? rule.FullDescription.Text
                        : rule.ShortDescription?.Text,
                    Details = details,
                };
            }
            catch (Exception ex) {
                Log.Error(ex, "Error creating annotation for result {@Result}",
                    new { physicalLocation, details, physicalLocation.Region?.StartLine });
                annotation = null;
            }

            if (annotation != null) yield return annotation;
        }
    }

    private string GetPathRelativeToCloneDir(ArtifactLocation artifactLocation, Run run)
    {
        var resultUri = artifactLocation.Uri;

        if (_cloneDirUri is null) return resultUri.ToString();

        Uri absoluteUri;

        if (resultUri.IsAbsoluteUri)
            absoluteUri = resultUri;
        else {
            if (string.IsNullOrEmpty(artifactLocation.UriBaseId) || run.OriginalUriBaseIds is null)
                return resultUri.ToString();

            bool gotBaseLocation =
                run.OriginalUriBaseIds.TryGetValue(artifactLocation.UriBaseId, out var baseLocation);

            if (!gotBaseLocation || baseLocation is null) return resultUri.ToString();

            Log.Debug("Base location: {BaseLocationUri}", baseLocation.Uri);

            absoluteUri = new Uri(baseLocation.Uri, resultUri);
        }

        Log.Debug("Absolute Uri: {AbsoluteUri}", absoluteUri);

        return _cloneDirUri.IsBaseOf(absoluteUri)
            ? _cloneDirUri.MakeRelativeUri(absoluteUri).ToString()
            : absoluteUri.ToString()[(_cloneDirUri.ToString().Length + 1)..];
    }
}
