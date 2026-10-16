using System.Text;

namespace ImpiantiTracker.Core.Reporting;

/// <summary>
/// Minimal CSV writing. The default separator is ';' because Excel set to Italian
/// uses ',' as the decimal mark and expects ';' between columns.
/// </summary>
public static class Csv
{
    public const char DefaultSeparator = ';';

    /// <summary>Quotes a value when it contains the separator, a quote or a line break; doubles inner quotes.</summary>
    public static string Escape(string? value, char separator = DefaultSeparator)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var needsQuotes = value.IndexOfAny([separator, '"', '\n', '\r']) >= 0;
        return needsQuotes ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }

    public static string Row(IEnumerable<string?> values, char separator = DefaultSeparator) =>
        string.Join(separator, values.Select(v => Escape(v, separator)));

    /// <summary>UTF-8 with a byte-order mark, so Excel shows accented letters (à, è, ò) correctly.</summary>
    public static Encoding ExcelEncoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
}
