using System.Globalization;

namespace ImpiantiTracker.Cli;

/// <summary>Values are shown the Italian way (16/10/2026, 1.234,50 €); labels stay in English.</summary>
public static class Formats
{
    private static readonly CultureInfo Italian = CultureInfo.GetCultureInfo("it-IT");

    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string Date(DateOnly? date) => date is { } d ? Date(d) : "never";

    public static string Money(decimal amount) => amount.ToString("#,##0.00 €", Italian);

    public static string Kw(decimal kw) => kw.ToString("0.0", Italian);

    public static string Month(int month) => CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month);

    public static string DaysFromToday(int days) => days switch
    {
        < 0 => $"{-days} d late",
        0 => "today",
        _ => $"in {days} d",
    };
}
