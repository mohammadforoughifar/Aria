BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Products]') AND [c].[name] = N'BasePriceToman');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Products] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [dbo].[Products] ALTER COLUMN [BasePriceToman] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Plans]') AND [c].[name] = N'PriceToman');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Plans] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [dbo].[Plans] ALTER COLUMN [PriceToman] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Payments]') AND [c].[name] = N'AmountToman');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Payments] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [dbo].[Payments] ALTER COLUMN [AmountToman] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Orders]') AND [c].[name] = N'TotalToman');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [dbo].[Orders] ALTER COLUMN [TotalToman] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Orders]') AND [c].[name] = N'TaxToman');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [dbo].[Orders] ALTER COLUMN [TaxToman] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Orders]') AND [c].[name] = N'SubtotalToman');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [dbo].[Orders] ALTER COLUMN [SubtotalToman] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Orders]') AND [c].[name] = N'DiscountToman');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [dbo].[Orders] ALTER COLUMN [DiscountToman] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[OrderItems]') AND [c].[name] = N'UnitPriceToman');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[OrderItems] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [dbo].[OrderItems] ALTER COLUMN [UnitPriceToman] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[OrderItems]') AND [c].[name] = N'LineTotalToman');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[OrderItems] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [dbo].[OrderItems] ALTER COLUMN [LineTotalToman] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Invoices]') AND [c].[name] = N'TotalToman');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Invoices] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [dbo].[Invoices] ALTER COLUMN [TotalToman] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[Invoices]') AND [c].[name] = N'TaxToman');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [dbo].[Invoices] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [dbo].[Invoices] ALTER COLUMN [TaxToman] decimal(18,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123045503_MoneyToDecimal'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261123045503_MoneyToDecimal', N'9.0.0');
END;

COMMIT;
GO

