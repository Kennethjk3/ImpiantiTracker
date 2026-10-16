-- The yearly report done in SQL instead of C#: better when there are many rows.
USE Impianti;
GO

DECLARE @Year int = YEAR(GETDATE());
DECLARE @From date = DATEFROMPARTS(@Year, 1, 1);
DECLARE @To   date = DATEFROMPARTS(@Year + 1, 1, 1);

-- A date range rather than YEAR([Date]) = @Year: a function on the column
-- would stop SQL Server from using an index on it (the condition must stay "sargable").

-- Visits and revenue by month
SELECT MONTH([Date]) AS [Month], COUNT(*) AS Visits, SUM(Cost) AS Revenue
FROM dbo.ServiceVisits
WHERE [Date] >= @From AND [Date] < @To
GROUP BY MONTH([Date])
ORDER BY [Month];

-- By technician, with a grand-total row from ROLLUP
SELECT
    CASE WHEN GROUPING(Technician) = 1 THEN N'TOTAL' ELSE Technician END AS Technician,
    COUNT(*)  AS Visits,
    SUM(Cost) AS Revenue
FROM dbo.ServiceVisits
WHERE [Date] >= @From AND [Date] < @To
GROUP BY ROLLUP (Technician)
ORDER BY GROUPING(Technician), Visits DESC;

-- The five customers who spent the most this year
SELECT TOP (5) c.Name, COUNT(*) AS Visits, SUM(v.Cost) AS Spent
FROM dbo.ServiceVisits AS v
JOIN dbo.Installations AS i ON i.Id = v.InstallationId
JOIN dbo.Customers AS c ON c.Id = i.CustomerId
WHERE v.[Date] >= @From AND v.[Date] < @To
GROUP BY c.Name
ORDER BY Spent DESC;
GO
