using ImpiantiTracker.Cli;

namespace ImpiantiTracker.Tests.Cli;

public class ArgParserTests
{
    private static ParsedArgs Parse(params string[] args) => ArgParser.Parse(args, CommandRunner.Flags);

    [Fact]
    public void Reads_command_positionals_options_and_flags()
    {
        var args = Parse("EXPORT", "due.csv", "--days", "60", "--all");

        Assert.Equal("export", args.Command);
        Assert.Equal(["due.csv"], args.Positionals);
        Assert.Equal(60, args.GetInt("days", 30));
        Assert.True(args.HasFlag("all"));
    }

    [Fact]
    public void Accepts_name_equals_value_and_any_case() =>
        Assert.Equal("Catanzaro", Parse("list", "--CITY=Catanzaro").Get("city"));

    [Fact]
    public void Falls_back_to_the_default_when_an_option_is_missing() =>
        Assert.Equal(30, Parse("due").GetInt("days", 30));

    [Fact]
    public void An_option_without_a_value_is_an_error() =>
        Assert.Throws<UsageException>(() => Parse("list", "--city"));

    [Fact]
    public void A_switch_cannot_take_a_value() =>
        Assert.Throws<UsageException>(() => Parse("due", "--all=yes"));

    [Theory]
    [InlineData("80,50")]
    [InlineData("80.50")]
    public void Costs_accept_both_decimal_marks(string text) =>
        Assert.Equal(80.50m, Parse("add-visit", "--cost", text).GetDecimal("cost", 0m));

    [Theory]
    [InlineData("2026-10-16")]
    [InlineData("16/10/2026")]
    public void Dates_accept_iso_and_italian_formats(string text) =>
        Assert.Equal(new DateOnly(2026, 10, 16), Parse("add-visit", "--date", text).GetDate("date", default));

    [Theory]
    [InlineData("31/02/2026")]
    [InlineData("yesterday")]
    public void Invalid_dates_are_rejected(string text) =>
        Assert.Throws<UsageException>(() => Parse("add-visit", "--date", text).GetDate("date", default));

    [Fact]
    public void Required_options_must_be_present() =>
        Assert.Throws<UsageException>(() => Parse("add-visit").Require("tech"));

    [Fact]
    public void Dash_h_asks_for_help() =>
        Assert.True(Parse("due", "-h").HasFlag("help"));
}

public class TablePrinterTests
{
    [Fact]
    public void Pads_every_column_to_its_widest_cell()
    {
        var lines = TablePrinter.Format(["ID", "Customer"], [TablePrinter.Row("1", "Bar Centrale"), TablePrinter.Row("12", "Hotel")]);

        Assert.Equal(
        [
            "ID  Customer",
            "--  ------------",
            "1   Bar Centrale",
            "12  Hotel",
        ], lines);
    }

    [Fact]
    public void Every_row_needs_one_cell_per_column() =>
        Assert.Throws<ArgumentException>(() => TablePrinter.Format(["A", "B"], [TablePrinter.Row("only one")]));
}
