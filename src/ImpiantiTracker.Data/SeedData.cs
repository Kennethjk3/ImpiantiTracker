using ImpiantiTracker.Core.Domain;

namespace ImpiantiTracker.Data;

/// <summary>
/// Fictional sample data. Dates are relative to "today", so the demo always shows
/// some installations overdue, some due soon and some fine, whenever it is run.
/// </summary>
public static class SeedData
{
    private const string Procopio = "A. Procopio";
    private const string Rotella = "G. Rotella";

    public static List<Customer> Create(DateOnly today)
    {
        DateOnly MonthsAgo(int months) => today.AddMonths(-months);

        // Last serviced a year ago minus N days, with a 12-month interval: due in N days.
        DateOnly DueInDays(int days) => today.AddMonths(-12).AddDays(days);

        return
        [
            new Customer
            {
                Name = "Condominio Le Palme", Address = "Via Lungomare 14", City = "Soverato",
                Phone = "0967 000 101", Email = "amministrazione@lepalme.example",
                Installations =
                [
                    new Installation
                    {
                        Type = InstallationType.HeatPump, Brand = "Daikin", Model = "Altherma 3 H HT", SerialNumber = "DK-48213",
                        PowerKw = 16m, InstalledOn = MonthsAgo(48), MaintenanceIntervalMonths = 12,
                        Visits =
                        [
                            Visit(MonthsAgo(25), VisitKind.Maintenance, Procopio, 140m, "Annual service"),
                            Visit(MonthsAgo(13), VisitKind.Maintenance, Procopio, 140m, "Filters cleaned, refrigerant pressure OK"),
                            // A repair does not reset the maintenance clock: this system is still overdue.
                            Visit(MonthsAgo(2), VisitKind.Repair, Rotella, 220m, "Replaced circulation pump"),
                        ],
                    },
                ],
            },
            new Customer
            {
                Name = "Ferraro Anna", Address = "Via Roma 8", City = "Catanzaro", Phone = "0961 000 102",
                Installations =
                [
                    new Installation
                    {
                        Type = InstallationType.Boiler, Brand = "Vaillant", Model = "ecoTEC plus", SerialNumber = "VL-77120",
                        PowerKw = 24m, InstalledOn = MonthsAgo(72), MaintenanceIntervalMonths = 12,
                        Visits =
                        [
                            Visit(MonthsAgo(24), VisitKind.EfficiencyCheck, Rotella, 120m, "RCEE submitted"),
                            Visit(DueInDays(14), VisitKind.Maintenance, Rotella, 110m, "Annual service, flue gas analysis OK"),
                            Visit(MonthsAgo(9), VisitKind.Repair, Procopio, 150m, "Pressure relief valve replaced"),
                        ],
                    },
                ],
            },
            new Customer
            {
                Name = "Pizzeria Il Gabbiano", Address = "Via del Mare 3", City = "Lamezia Terme", Phone = "0968 000 103",
                Installations =
                [
                    new Installation
                    {
                        Type = InstallationType.PelletStove, Brand = "MCZ", Model = "Club Comfort Air", SerialNumber = "MCZ-30551",
                        PowerKw = 9m, InstalledOn = MonthsAgo(30), MaintenanceIntervalMonths = 12,
                        Visits =
                        [
                            Visit(MonthsAgo(15), VisitKind.Maintenance, Procopio, 90m, "Seasonal cleaning"),
                            Visit(MonthsAgo(3), VisitKind.Maintenance, Procopio, 95m, "Burner pot and flue cleaned"),
                        ],
                    },
                    new Installation
                    {
                        Type = InstallationType.AirConditioner, Brand = "Mitsubishi Electric", Model = "MSZ-LN35", SerialNumber = "ME-10442",
                        PowerKw = 3.5m, InstalledOn = MonthsAgo(11), MaintenanceIntervalMonths = 24,
                        Visits =
                        [
                            Visit(MonthsAgo(4), VisitKind.Repair, Rotella, 45m, "Remote control replaced"),
                        ],
                    },
                ],
            },
            new Customer
            {
                Name = "Studio Medico Greco", Address = "Corso Mazzini 120", City = "Catanzaro", Phone = "0961 000 104",
                Installations =
                [
                    new Installation
                    {
                        Type = InstallationType.AirConditioner, Brand = "Daikin", Model = "Perfera FTXM50", SerialNumber = "DK-50918",
                        PowerKw = 5m, InstalledOn = MonthsAgo(40), MaintenanceIntervalMonths = 12,
                        Visits =
                        [
                            Visit(MonthsAgo(26), VisitKind.Maintenance, Rotella, 85m, "Filters and coil cleaned"),
                            Visit(MonthsAgo(14), VisitKind.Maintenance, Rotella, 85m, "Filters and coil cleaned"),
                            Visit(MonthsAgo(6), VisitKind.Repair, Procopio, 70m, "Condensate drain unblocked"),
                        ],
                    },
                ],
            },
            new Customer
            {
                Name = "Mancuso Luca", Address = "Via Garibaldi 21", City = "Satriano", Phone = "0967 000 105",
                Installations =
                [
                    // Never serviced yet: due one interval after installation, in 25 days.
                    new Installation
                    {
                        Type = InstallationType.Boiler, Brand = "Ariston", Model = "Genus One", SerialNumber = "AR-66310",
                        PowerKw = 28m, InstalledOn = DueInDays(25), MaintenanceIntervalMonths = 12, RegistryCode = "DEMO-0001",
                    },
                ],
            },
            new Customer
            {
                Name = "Hotel Belvedere", Address = "Via Panoramica 7", City = "Squillace", Phone = "0961 000 106",
                Email = "manutenzione@hotelbelvedere.example",
                Installations =
                [
                    new Installation
                    {
                        Type = InstallationType.HeatPump, Brand = "Daikin", Model = "EWYT-CZ", SerialNumber = "DK-90021",
                        PowerKw = 45m, InstalledOn = MonthsAgo(36), MaintenanceIntervalMonths = 6,
                        Visits =
                        [
                            Visit(MonthsAgo(13), VisitKind.Maintenance, Procopio, 260m, "Six-monthly service"),
                            Visit(MonthsAgo(7), VisitKind.EfficiencyCheck, Rotella, 180m, "RCEE submitted"),
                        ],
                    },
                    new Installation
                    {
                        Type = InstallationType.SolarThermal, Brand = "Vaillant", Model = "auroTHERM", SerialNumber = "VL-40417",
                        PowerKw = 4m, InstalledOn = MonthsAgo(36), MaintenanceIntervalMonths = 24,
                        Visits =
                        [
                            Visit(MonthsAgo(5), VisitKind.Maintenance, Procopio, 120m, "Glycol checked and topped up"),
                        ],
                    },
                ],
            },
            new Customer
            {
                Name = "Bar Centrale", Address = "Piazza del Popolo 2", City = "Girifalco", Phone = "0968 000 107",
                Installations =
                [
                    // Due in 45 days: not in the default 30-day list, but shown with --days 60.
                    new Installation
                    {
                        Type = InstallationType.AirConditioner, Brand = "Mitsubishi Electric", Model = "MSZ-AP25", SerialNumber = "ME-20577",
                        PowerKw = 2.5m, InstalledOn = MonthsAgo(20), MaintenanceIntervalMonths = 12,
                        Visits =
                        [
                            Visit(DueInDays(45), VisitKind.Maintenance, Rotella, 80m, "Annual service"),
                            Visit(MonthsAgo(1), VisitKind.Repair, Procopio, 60m, "Refrigerant top-up"),
                        ],
                    },
                ],
            },
        ];
    }

    private static ServiceVisit Visit(DateOnly date, VisitKind kind, string technician, decimal cost, string notes) =>
        new() { Date = date, Kind = kind, Technician = technician, Cost = cost, Notes = notes };
}
