using System;
using System.Collections.Generic;
using System.Linq;
using DiffPatchFixed.Data;

namespace DiffPatchFixed;

public static class DiffParserHelper
{
    public static IEnumerable<FileDiff> Parse(string? input, string lineEnding = "\n")
    {
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<FileDiff>();

        string[] lines = StringHelper.SplitLines(input, lineEnding);

        if (!lines.Any())
            return [];

        var parser = new DiffParser();

        return parser.Run(lines);
    }
}
