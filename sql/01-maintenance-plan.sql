-- The maintenance plan in plain T-SQL: the same rule as MaintenanceSchedule.cs.
-- Next due = last routine service (or the installation date, if never serviced) + the interval.
-- Repairs don't count as servicing.
USE Impianti;
GO

CREATE OR ALTER VIEW dbo.vw_MaintenancePlan
AS
WITH LastService AS (
    SELECT InstallationId, MAX([Date]) AS LastServiceDate
    FROM dbo.ServiceVisits
    WHERE Kind IN (N'Maintenance', N'EfficiencyCheck')
    GROUP BY InstallationId
)
SELECT
    i.Id            AS InstallationId,
    c.Name          AS Customer,
    c.City,
    c.Phone,
    i.[Type],
    i.Brand,
    ls.LastServiceDate,
    -- DATEADD clamps to the end of the month (31 Jan + 1 month = 28 Feb), like DateOnly.AddMonths.
    DATEADD(MONTH, i.MaintenanceIntervalMonths,
        CASE WHEN ls.LastServiceDate > i.InstalledOn THEN ls.LastServiceDate ELSE i.InstalledOn END) AS NextDue
FROM dbo.Installations AS i
JOIN dbo.Customers AS c ON c.Id = i.CustomerId
LEFT JOIN LastService AS ls ON ls.InstallationId = i.Id;   -- LEFT: keep installations never serviced
GO

DECLARE @Today date = CAST(GETDATE() AS date);
DECLARE @WarningDays int = 30;

SELECT
    InstallationId,
    CASE
        WHEN NextDue < @Today THEN 'OVERDUE'
        WHEN NextDue <= DATEADD(DAY, @WarningDays, @Today) THEN 'Due soon'
        ELSE 'OK'
    END AS [Status],
    NextDue,
    DATEDIFF(DAY, @Today, NextDue) AS DaysUntilDue,
    Customer,
    City,
    Phone,
    [Type],
    LastServiceDate
FROM dbo.vw_MaintenancePlan
WHERE NextDue <= DATEADD(DAY, @WarningDays, @Today)
ORDER BY NextDue, Customer;
GO
