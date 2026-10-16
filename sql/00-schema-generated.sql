IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016084250_InitialCreate'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(120) NULL,
        [Address] nvarchar(200) NOT NULL,
        [City] nvarchar(80) NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016084250_InitialCreate'
)
BEGIN
    CREATE TABLE [Installations] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [Type] nvarchar(32) NOT NULL,
        [Brand] nvarchar(60) NOT NULL,
        [Model] nvarchar(80) NULL,
        [SerialNumber] nvarchar(60) NULL,
        [PowerKw] decimal(6,2) NOT NULL,
        [InstalledOn] date NOT NULL,
        [MaintenanceIntervalMonths] int NOT NULL,
        [RegistryCode] nvarchar(40) NULL,
        CONSTRAINT [PK_Installations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Installations_Interval] CHECK ([MaintenanceIntervalMonths] BETWEEN 1 AND 60),
        CONSTRAINT [FK_Installations_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016084250_InitialCreate'
)
BEGIN
    CREATE TABLE [ServiceVisits] (
        [Id] int NOT NULL IDENTITY,
        [InstallationId] int NOT NULL,
        [Date] date NOT NULL,
        [Kind] nvarchar(32) NOT NULL,
        [Technician] nvarchar(80) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [Cost] decimal(10,2) NOT NULL,
        CONSTRAINT [PK_ServiceVisits] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ServiceVisits_Cost] CHECK ([Cost] >= 0),
        CONSTRAINT [FK_ServiceVisits_Installations_InstallationId] FOREIGN KEY ([InstallationId]) REFERENCES [Installations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016084250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Customers_Name] ON [Customers] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016084250_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Installations_Brand_SerialNumber] ON [Installations] ([Brand], [SerialNumber]) WHERE [SerialNumber] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016084250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Installations_CustomerId] ON [Installations] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016084250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ServiceVisits_InstallationId_Date] ON [ServiceVisits] ([InstallationId], [Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016084250_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016084250_InitialCreate', N'10.0.13');
END;

COMMIT;
GO

