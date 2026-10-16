namespace ImpiantiTracker.Cli;

public static class Help
{
    public const string Text = """
        impianti: track heating and cooling installations and their maintenance

        Usage: dotnet run --project src/ImpiantiTracker.Cli -- <command> [options]

        Commands
          init                     Create the database and load the sample data (safe to re-run)
          reset --yes              Delete the database and start again with the sample data
          list [--city <name>]     Installations with their customer and last visit
          show <id>                One installation with its full visit history
          due [--days N] [--all]   Maintenance plan: overdue first, then due within N days
                                   (default from appsettings.json; --all includes the rest)
          add-visit                Record a visit:
              --installation <id> --kind <maintenance|rcee|repair> --tech <name>
              [--date 2026-10-16] [--cost 80,50] [--notes "text"]
          report [--year YYYY]     Visits and revenue by month, technician and type
          export <file.csv> [--days N] [--all]
                                   Maintenance plan as CSV for Excel (';' separated, UTF-8)
          help                     This text

        Settings come from appsettings.json and can be overridden with environment variables
        prefixed IMPIANTI_, for example:
          env IMPIANTI_Database__Provider=SqlServer dotnet run --project src/ImpiantiTracker.Cli -- due
        """;
}
