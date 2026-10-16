using System.Globalization;

namespace ImpiantiTracker.Cli;

/// <summary>A mistake in how the command was typed. Shown to the user without a stack trace.</summary>
public sealed class UsageException(string message) : Exception(message);

public sealed class ParsedArgs
{
    private readonly Dictionary<string, string> _options;
    private readonly HashSet<string> _flags;

    internal ParsedArgs(string? command, List<string> positionals, Dictionary<string, string> options, HashSet<string> flags)
    {
        Command = command;
        Positionals = positionals;
        _options = options;
        _flags = flags;
    }

    /// <summary>The first word, lower-cased: "due", "list", "add-visit"…</summary>
    public string? Command { get; }
    public IReadOnlyList<string> Positionals { get; }

    public bool HasFlag(string name) => _flags.Contains(name);

    public string? Get(string name) => _options.GetValueOrDefault(name);

    public string Require(string name) =>
        Get(name) is { Length: > 0 } value ? value : throw new UsageException($"Missing required option --{name}.");

    public int GetInt(string name, int fallback) => Get(name) is { } text
        ? int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new UsageException($"--{name} must be a whole number, not '{text}'.")
        : fallback;

    /// <summary>Accepts 80, 80.50 or 80,50 (the Italian decimal comma).</summary>
    public decimal GetDecimal(string name, decimal fallback) => Get(name) is { } text
        ? decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new UsageException($"--{name} must be a number, not '{text}'.")
        : fallback;

    /// <summary>Accepts 2026-10-16 or 16/10/2026.</summary>
    public DateOnly GetDate(string name, DateOnly fallback) => Get(name) is { } text
        ? DateOnly.TryParseExact(text, ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : throw new UsageException($"--{name} must be a date like 2026-10-16 or 16/10/2026, not '{text}'.")
        : fallback;
}

/// <summary>
/// A deliberately small parser: "command positional --option value --option=value --flag".
/// Option names are case-insensitive; a repeated option keeps its last value.
/// </summary>
public static class ArgParser
{
    public static ParsedArgs Parse(IReadOnlyList<string> args, IEnumerable<string> flagNames)
    {
        var knownFlags = new HashSet<string>(flagNames, StringComparer.OrdinalIgnoreCase);
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var positionals = new List<string>();
        string? command = null;

        for (var i = 0; i < args.Count; i++)
        {
            var token = args[i];

            if (token is "-h" or "--help")
            {
                flags.Add("help");
                continue;
            }

            if (token.StartsWith("--", StringComparison.Ordinal) && token.Length > 2)
            {
                var name = token[2..];
                string? inlineValue = null;
                var equals = name.IndexOf('=');
                if (equals >= 0)
                {
                    inlineValue = name[(equals + 1)..];
                    name = name[..equals];
                }

                if (knownFlags.Contains(name))
                {
                    if (inlineValue is not null)
                        throw new UsageException($"--{name} is a switch and takes no value.");
                    flags.Add(name);
                }
                else if (inlineValue is not null)
                {
                    options[name] = inlineValue;
                }
                else if (i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    options[name] = args[++i];
                }
                else
                {
                    throw new UsageException($"Option --{name} needs a value.");
                }
                continue;
            }

            if (command is null)
                command = token.ToLowerInvariant();
            else
                positionals.Add(token);
        }

        return new ParsedArgs(command, positionals, options, flags);
    }
}
