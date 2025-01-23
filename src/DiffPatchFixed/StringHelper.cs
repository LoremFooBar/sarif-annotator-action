using System;

namespace DiffPatchFixed;

public static class StringHelper
{
    public static string[] SplitLines(string? input, string lineEnding)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        string[] lines = input!.Split([lineEnding], StringSplitOptions.None);

        return lines.Length == 0 ? [] : lines;
    }
}
