namespace ImpiantiTracker.Cli;

/// <summary>Prints rows as aligned columns, optionally coloring a row (only on a real terminal).</summary>
public static class TablePrinter
{
    /// <summary>One table row: TablePrinter.Row("1", "Ferraro Anna", "Catanzaro").</summary>
    public static IReadOnlyList<string> Row(params string[] cells) => cells;

    public static IReadOnlyList<string> Format(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        foreach (var row in rows)
        {
            if (row.Count != headers.Count)
                throw new ArgumentException($"Every row needs {headers.Count} cells; one has {row.Count}.", nameof(rows));
        }

        var widths = headers
            .Select((header, column) => rows.Select(r => r[column].Length).Prepend(header.Length).Max())
            .ToArray();

        string Line(IReadOnlyList<string> cells) =>
            string.Join("  ", cells.Select((cell, column) => cell.PadRight(widths[column]))).TrimEnd();

        var lines = new List<string>(rows.Count + 2)
        {
            Line(headers),
            string.Join("  ", widths.Select(w => new string('-', w))),
        };
        lines.AddRange(rows.Select(Line));
        return lines;
    }

    public static void Print(
        TextWriter output,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        Func<int, ConsoleColor?>? rowColor = null)
    {
        var lines = Format(headers, rows);
        var useColor = rowColor is not null && ReferenceEquals(output, Console.Out) && !Console.IsOutputRedirected;

        for (var i = 0; i < lines.Count; i++)
        {
            var color = useColor && i >= 2 ? rowColor!(i - 2) : null;
            if (color is { } c) Console.ForegroundColor = c;
            output.WriteLine(lines[i]);
            if (color is not null) Console.ResetColor();
        }
    }
}
