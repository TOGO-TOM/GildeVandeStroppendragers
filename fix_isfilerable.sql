IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260402101122_AddCustomFieldIsFilterable')
BEGIN
    ALTER TABLE [CustomFields] ADD [IsFilterable] bit NOT NULL DEFAULT CAST(0 AS bit);
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260402101122_AddCustomFieldIsFilterable', N'8.0.0');
END
