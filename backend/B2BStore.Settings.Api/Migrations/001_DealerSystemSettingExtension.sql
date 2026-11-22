/*
  Explicit application-owned settings migration.
  Apply manually only to Store5_Settings_Test after a checksum-verified backup.
  This table is not part of the legacy B2B schema and is not read by the old portal.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'Store5_Settings_Test'
    THROW 51000, 'This migration is restricted to Store5_Settings_Test.', 1;

IF OBJECT_ID(N'dbo.DealerSettings', N'U') IS NULL
    THROW 51001, 'dbo.DealerSettings is not present in the target database.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.DealerSettings')
      AND c.name = N'MemberCode'
      AND t.name = N'nvarchar'
      AND c.max_length = 200
)
    THROW 51002, 'DealerSettings.MemberCode does not match the verified nvarchar(100) shape.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.DealerSettings WHERE MemberCode = N'store5') <> 1
    THROW 51003, 'Exactly one approved store5 settings row is required before migration.', 1;

IF OBJECT_ID(N'dbo.DealerSystemSettingExtension', N'U') IS NULL
BEGIN
    BEGIN TRANSACTION;
    CREATE TABLE dbo.DealerSystemSettingExtension
    (
        MemberCode nvarchar(100) NOT NULL,
        SettingKey nvarchar(128) NOT NULL,
        SettingValue nvarchar(128) NOT NULL,
        ValueType varchar(24) NOT NULL,
        UpdatedAtUtc datetime2(7) NOT NULL CONSTRAINT DF_DealerSystemSettingExtension_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_DealerSystemSettingExtension PRIMARY KEY (MemberCode, SettingKey)
    );
    COMMIT TRANSACTION;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'dbo.DealerSystemSettingExtension')
      AND i.is_unique = 1
      AND i.name = N'PK_DealerSystemSettingExtension'
)
    THROW 51004, 'The extension table does not have its expected unique primary key.', 1;

PRINT 'DealerSystemSettingExtension migration verified for Store5_Settings_Test.';
