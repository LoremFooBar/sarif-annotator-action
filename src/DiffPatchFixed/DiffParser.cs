using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DiffPatchFixed.Data;

namespace DiffPatchFixed;

internal class DiffParser
{
    private const string noeol = "\\ No newline at end of file";
    private const string devnull = "/dev/null";

    private static readonly Regex FileNameRegex = new(@"^(a|b)\/", RegexOptions.Compiled);

    private static readonly Regex TimestampRegex =
        new(@"\t.*|\d{4}-\d\d-\d\d\s\d\d:\d\d:\d\d(.\d+)?\s(\+|-)\d\d\d\d", RegexOptions.Compiled);

    private static readonly Regex GitPrefixRegex = new(@"^(a|b)\/", RegexOptions.Compiled);

    private readonly List<FileDiff> files = [];

    private readonly HandlerCollection schema;

    private Chunk? current;
    private FileDiff? file;
    private int in_del, in_add;
    private int oldLines, newLines;

    private int oldStart, newStart;

    public DiffParser() =>
        schema = new HandlerCollection
        {
            { @"^diff\s", Start },
            { @"^new file mode \d+$", NewFile },
            { @"^deleted file mode \d+$", DeletedFile },
            { @"^index\s[\da-zA-Z]+\.\.[\da-zA-Z]+(\s(\d+))?$", Index },
            { @"^---\s", FromFile },
            { @"^\+\+\+\s", ToFile },
            { @"^@@\s+\-(\d+),?(\d+)?\s+\+(\d+),?(\d+)?\s@@", Chunk },
            { "^-", DeleteLine },
            { @"^\+", AddLine },
            { "^Binary files (.+) and (.+) differ", BinaryDiff },
        };

    public IEnumerable<FileDiff> Run(IEnumerable<string> lines)
    {
        foreach (string? line in lines) {
            string trimmedLine = line;

            if (trimmedLine.EndsWith("\r")) trimmedLine = line.Substring(0, line.Length - 1);

            if (!ParseLine(trimmedLine)) ParseNormalLine(trimmedLine);
        }

        return files;
    }

    private void Start(string? line)
    {
        file = new FileDiff();
        files.Add(file);

        if (file.To == null && file.From == null) {
            string[]? fileNames = ParseFileNames(line);

            if (fileNames != null) {
                file.From = fileNames[0];
                file.To = fileNames[1];
            }
        }
    }

    private void Restart()
    {
        if (file == null || file.Chunks.Count != 0)
            Start(null);
    }

    private void NewFile()
    {
        Restart();

        if (file is not null) {
            file.Type = FileChangeType.Add;
            file.From = devnull;
        }
    }

    private void DeletedFile()
    {
        Restart();

        if (file is not null) {
            file.Type = FileChangeType.Delete;
            file.To = devnull;
        }
    }

    private void Index(string line)
    {
        Restart();

        if (file is not null) file.Index = line.Split(' ').Skip(1);
    }

    private void FromFile(string line)
    {
        Restart();

        if (file is not null) file.From = ParseFileName(line);
    }

    private void ToFile(string line)
    {
        Restart();

        if (file is not null) file.To = ParseFileName(line);
    }

    private void BinaryDiff()
    {
        Restart();

        if (file is not null) file.Type = FileChangeType.Modified;
    }

    private void Chunk(string line, Match match)
    {
        in_del = oldStart = int.Parse(match.Groups[1].Value);
        oldLines = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        in_add = newStart = int.Parse(match.Groups[3].Value);
        newLines = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
        var rangeInfo = new ChunkRangeInfo(
            new ChunkRange(oldStart, oldLines),
            new ChunkRange(newStart, newLines)
        );

        current = new Chunk(line, rangeInfo);
        file?.Chunks.Add(current);
    }

    private void DeleteLine(string line)
    {
        string content = DiffLineHelper.GetContent(line);
        current?.Changes.Add(new LineDiff(LineChangeType.Delete, in_del++, content));

        if (file is not null) file.Deletions++;
    }

    private void AddLine(string line)
    {
        string content = DiffLineHelper.GetContent(line);
        current?.Changes.Add(new LineDiff(LineChangeType.Add, in_add++, content));

        if (file is not null) file.Additions++;
    }

    private void ParseNormalLine(string line)
    {
        if (file is null || string.IsNullOrEmpty(line) || current is null) return;

        string content = DiffLineHelper.GetContent(line);
        current.Changes.Add(new LineDiff(
            line == noeol ? 0 : in_del++,
            line == noeol ? 0 : in_add++,
            content));
    }

    private bool ParseLine(string line)
    {
        foreach (var p in schema) {
            var m = p.Expression.Match(line);

            if (m.Success) {
                p.Action(line, m);

                return true;
            }
        }

        return false;
    }

    private static string[]? ParseFileNames(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;

        return s!
            .Split(' ')
            .Reverse().Take(2).Reverse()
            .Select(fileName => FileNameRegex.Replace(fileName, string.Empty)).ToArray();
    }

    private static string ParseFileName(string s)
    {
        s = s.TrimStart('-', '+');
        s = s.Trim();

        // ignore possible time stamp
        var t = TimestampRegex.Match(s);
        if (t.Success) s = s.Substring(0, t.Index).Trim();

        // ignore git prefixes a/ or b/
        return GitPrefixRegex.IsMatch(s) ? s.Substring(2) : s;
    }

    private class HandlerRow
    {
        public HandlerRow(Regex expression, Action<string, Match> action)
        {
            Expression = expression;
            Action = action;
        }

        public Regex Expression { get; }

        public Action<string, Match> Action { get; }
    }

    private class HandlerCollection : IEnumerable<HandlerRow>
    {
        private readonly List<HandlerRow> handlers = new();

        public IEnumerator<HandlerRow> GetEnumerator() => handlers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => handlers.GetEnumerator();

        public void Add(string expression, Action action)
        {
            handlers.Add(new HandlerRow(new Regex(expression), (_, _) => action()));
        }

        public void Add(string expression, Action<string> action)
        {
            handlers.Add(new HandlerRow(new Regex(expression), (line, _) => action(line)));
        }

        public void Add(string expression, Action<string, Match> action)
        {
            handlers.Add(new HandlerRow(new Regex(expression), action));
        }
    }
}
