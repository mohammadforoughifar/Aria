IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Articles] (
        [Id] uniqueidentifier NOT NULL,
        [Slug] nvarchar(160) NOT NULL,
        [Title] nvarchar(240) NOT NULL,
        [Excerpt] nvarchar(600) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Category] nvarchar(80) NULL,
        [IsPublished] bit NOT NULL,
        [PublishedAt] datetime2(3) NOT NULL,
        [ReadMinutes] int NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Articles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Categories] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Slug] nvarchar(140) NOT NULL,
        [Description] nvarchar(600) NULL,
        [Icon] nvarchar(64) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[ContactMessages] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [Company] nvarchar(200) NULL,
        [Topic] nvarchar(80) NOT NULL,
        [Message] nvarchar(4000) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[DiscountCodes] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Description] nvarchar(400) NULL,
        [PercentOff] int NOT NULL,
        [MaxRedemptions] int NOT NULL,
        [TimesRedeemed] int NOT NULL,
        [ExpiresAt] datetime2(3) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DiscountCodes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Faqs] (
        [Id] uniqueidentifier NOT NULL,
        [Question] nvarchar(400) NOT NULL,
        [Answer] nvarchar(4000) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Faqs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[SiteSettings] (
        [Id] int NOT NULL,
        [SiteName] nvarchar(160) NOT NULL,
        [SupportEmail] nvarchar(256) NOT NULL,
        [SalesEmail] nvarchar(256) NOT NULL,
        [SupportPhone] nvarchar(32) NOT NULL,
        [HeroHeadline] nvarchar(240) NOT NULL,
        [HeroSubheading] nvarchar(600) NOT NULL,
        [TaxRateBps] int NOT NULL,
        [TrialDays] int NOT NULL,
        [MaintenanceMode] bit NOT NULL,
        [Announcement] nvarchar(400) NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Testimonials] (
        [Id] uniqueidentifier NOT NULL,
        [AuthorName] nvarchar(160) NOT NULL,
        [AuthorRole] nvarchar(160) NOT NULL,
        [Company] nvarchar(200) NOT NULL,
        [Quote] nvarchar(1000) NOT NULL,
        [Rating] int NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Testimonials] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [Phone] nvarchar(32) NULL,
        [FullName] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [Role] int NOT NULL,
        [AccountType] int NOT NULL,
        [Locked] bit NOT NULL,
        [Company] nvarchar(200) NULL,
        [Country] nvarchar(80) NULL,
        [EconomicCode] nvarchar(32) NULL,
        [NationalId] nvarchar(32) NULL,
        [LastLoginAt] datetime2(3) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Products] (
        [Id] uniqueidentifier NOT NULL,
        [Slug] nvarchar(140) NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Tagline] nvarchar(240) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Type] int NOT NULL,
        [Audience] int NOT NULL,
        [Platform] nvarchar(40) NOT NULL,
        [Version] nvarchar(32) NOT NULL,
        [BasePriceToman] int NOT NULL,
        [AccentColor] nvarchar(16) NOT NULL,
        [HeroImage] nvarchar(400) NULL,
        [IsFeatured] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [TrialDays] int NOT NULL,
        [Popularity] int NOT NULL,
        [Features] nvarchar(max) NOT NULL,
        [Requirements] nvarchar(max) NOT NULL,
        [Gallery] nvarchar(max) NOT NULL,
        [DemoVideoUrl] nvarchar(400) NULL,
        [CategoryId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Title] nvarchar(240) NOT NULL,
        [Body] nvarchar(2000) NULL,
        [Kind] nvarchar(32) NOT NULL,
        [IsRead] bit NOT NULL,
        [Link] nvarchar(400) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Orders] (
        [Id] uniqueidentifier NOT NULL,
        [OrderNumber] nvarchar(32) NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [SubtotalToman] int NOT NULL,
        [DiscountToman] int NOT NULL,
        [TaxToman] int NOT NULL,
        [TotalToman] int NOT NULL,
        [Currency] nvarchar(8) NOT NULL,
        [DiscountCode] nvarchar(64) NULL,
        [PlacedAt] datetime2(3) NOT NULL,
        [AccountType] int NOT NULL,
        [BillingName] nvarchar(200) NULL,
        [BillingCompany] nvarchar(200) NULL,
        [BillingEmail] nvarchar(256) NOT NULL,
        [BillingCountry] nvarchar(80) NULL,
        [EconomicCode] nvarchar(32) NULL,
        [NationalId] nvarchar(32) NULL,
        [Notes] nvarchar(2000) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[DemoRequests] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NULL,
        [Name] nvarchar(160) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [Company] nvarchar(200) NULL,
        [CompanySize] nvarchar(40) NULL,
        [Phone] nvarchar(32) NULL,
        [Message] nvarchar(2000) NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DemoRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DemoRequests_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Downloads] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [VersionId] uniqueidentifier NULL,
        [Version] nvarchar(32) NOT NULL,
        [Kind] nvarchar(32) NOT NULL,
        [FileName] nvarchar(260) NOT NULL,
        [SizeMb] int NOT NULL,
        [RequiresLicense] bit NOT NULL,
        [DownloadedAt] datetime2(3) NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Downloads] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Downloads_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Plans] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Tier] int NOT NULL,
        [BillingType] int NOT NULL,
        [PriceToman] int NOT NULL,
        [Seats] int NOT NULL,
        [Description] nvarchar(600) NULL,
        [IsPopular] bit NOT NULL,
        [LicenseMonths] int NOT NULL,
        [Features] nvarchar(max) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Plans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Plans_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[ProductVersions] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [Version] nvarchar(32) NOT NULL,
        [ReleasedAt] datetime2(3) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [InstallerUrl] nvarchar(400) NULL,
        [SizeMb] int NOT NULL,
        [Platform] nvarchar(40) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ProductVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductVersions_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Reviews] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [AuthorName] nvarchar(160) NOT NULL,
        [AuthorCompany] nvarchar(200) NULL,
        [Rating] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Tickets] (
        [Id] uniqueidentifier NOT NULL,
        [Reference] nvarchar(32) NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NULL,
        [Subject] nvarchar(240) NOT NULL,
        [Category] nvarchar(80) NOT NULL,
        [Priority] int NOT NULL,
        [Status] int NOT NULL,
        [Environment] nvarchar(400) NULL,
        [LastActivityAt] datetime2(3) NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Tickets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tickets_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Tickets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Invoices] (
        [Id] uniqueidentifier NOT NULL,
        [InvoiceNumber] nvarchar(32) NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [IssuedAt] datetime2(3) NOT NULL,
        [DueAt] datetime2(3) NULL,
        [TotalToman] int NOT NULL,
        [TaxToman] int NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[OrderItems] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [PlanId] uniqueidentifier NOT NULL,
        [ProductName] nvarchar(160) NOT NULL,
        [PlanName] nvarchar(120) NOT NULL,
        [BillingType] int NOT NULL,
        [UnitPriceToman] int NOT NULL,
        [Quantity] int NOT NULL,
        [LineTotalToman] int NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Payments] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [Provider] nvarchar(64) NOT NULL,
        [Method] nvarchar(64) NOT NULL,
        [AmountToman] int NOT NULL,
        [Status] int NOT NULL,
        [TransactionRef] nvarchar(128) NULL,
        [PaidAt] datetime2(3) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[CartItems] (
        [Id] uniqueidentifier NOT NULL,
        [SessionKey] nvarchar(64) NOT NULL,
        [UserId] uniqueidentifier NULL,
        [PlanId] uniqueidentifier NOT NULL,
        [Quantity] int NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CartItems_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [dbo].[Plans] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Licenses] (
        [Id] uniqueidentifier NOT NULL,
        [LicenseKey] nvarchar(64) NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [PlanId] uniqueidentifier NULL,
        [Seats] int NOT NULL,
        [ActivationsUsed] int NOT NULL,
        [Status] int NOT NULL,
        [IssuedAt] datetime2(3) NOT NULL,
        [ExpiresAt] datetime2(3) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Licenses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Licenses_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [dbo].[Plans] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Licenses_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Licenses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[TicketMessages] (
        [Id] uniqueidentifier NOT NULL,
        [TicketId] uniqueidentifier NOT NULL,
        [AuthorName] nvarchar(160) NOT NULL,
        [IsStaff] bit NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_TicketMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TicketMessages_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [dbo].[Tickets] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Articles_Published_Date] ON [dbo].[Articles] ([IsPublished], [PublishedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Articles_Slug] ON [dbo].[Articles] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CartItems_PlanId] ON [dbo].[CartItems] ([PlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CartItems_Session] ON [dbo].[CartItems] ([SessionKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CartItems_User] ON [dbo].[CartItems] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CartItems_Session_Plan_User] ON [dbo].[CartItems] ([SessionKey], [PlanId], [UserId]) WHERE [UserId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Categories_SortOrder] ON [dbo].[Categories] ([SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Categories_Slug] ON [dbo].[Categories] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ContactMessages_Read_Date] ON [dbo].[ContactMessages] ([IsRead], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DemoRequests_ProductId] ON [dbo].[DemoRequests] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DemoRequests_Status_Date] ON [dbo].[DemoRequests] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DiscountCodes_Active] ON [dbo].[DiscountCodes] ([IsActive], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_DiscountCodes_Code] ON [dbo].[DiscountCodes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Downloads_Product] ON [dbo].[Downloads] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Downloads_User_Date] ON [dbo].[Downloads] ([UserId], [DownloadedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Faqs_SortOrder] ON [dbo].[Faqs] ([SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Invoices_Number] ON [dbo].[Invoices] ([InvoiceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Invoices_Order] ON [dbo].[Invoices] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Licenses_Expires] ON [dbo].[Licenses] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Licenses_PlanId] ON [dbo].[Licenses] ([PlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Licenses_ProductId] ON [dbo].[Licenses] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Licenses_User_Status] ON [dbo].[Licenses] ([UserId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Licenses_Key] ON [dbo].[Licenses] ([LicenseKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_User_Read_Date] ON [dbo].[Notifications] ([UserId], [IsRead], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_Order] ON [dbo].[OrderItems] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_Status] ON [dbo].[Orders] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_User_Placed] ON [dbo].[Orders] ([UserId], [PlacedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Orders_Number] ON [dbo].[Orders] ([OrderNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_Status] ON [dbo].[Payments] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Payments_Order] ON [dbo].[Payments] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Plans_Product_Sort] ON [dbo].[Plans] ([ProductId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Category_Active] ON [dbo].[Products] ([CategoryId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Featured_Popularity] ON [dbo].[Products] ([IsFeatured], [Popularity]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Products_Slug] ON [dbo].[Products] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductVersions_Product_Released] ON [dbo].[ProductVersions] ([ProductId], [ReleasedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reviews_Product_Status] ON [dbo].[Reviews] ([ProductId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reviews_Status] ON [dbo].[Reviews] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TicketMessages_Ticket_Date] ON [dbo].[TicketMessages] ([TicketId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_ProductId] ON [dbo].[Tickets] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_Status] ON [dbo].[Tickets] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_User_Activity] ON [dbo].[Tickets] ([UserId], [LastActivityAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Tickets_Reference] ON [dbo].[Tickets] ([Reference]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_Role_Locked] ON [dbo].[Users] ([Role], [Locked]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Users_Email] ON [dbo].[Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122073650_InitialCreate'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122073650_InitialCreate', N'9.0.0');
END;

COMMIT;
GO

