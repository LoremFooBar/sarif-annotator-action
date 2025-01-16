using Microsoft.CodeAnalysis.Sarif;

namespace LoremFooBar.SarifAnnotatorAction.Utils;

public static class SarifLogExtensions
{
    public static List<ResultWithRun> FlatResults(this SarifLog sarif) =>
        sarif.Runs.SelectMany(r => r.Results.Select(rr => new ResultWithRun(r, rr))).ToList();
}

public record ResultWithRun(Run Run, Result Result);
