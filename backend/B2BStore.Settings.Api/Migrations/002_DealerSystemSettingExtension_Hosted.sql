/* Hosted application-owned settings extension table.
   Run only against the MonsterASP database db82141.
   This table belongs to this management application; the legacy portal does not read it. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'db82141'
    THROW 51000, 'This migration is restricted to database db82141.', 1;

IF OBJECT_ID(N'dbo.DealerSettings', N'U') IS NULL
    THROW 51001, 'dbo.DealerSettings is not present in the target database.', 1;

IF COL_LENGTH(N'dbo.DealerSettings', N'MemberCode') IS NULL
    THROW 51002, 'DealerSettings.MemberCode is missing.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.DealerSettings WHERE MemberCode = N'store5') <> 1
    THROW 51003, 'Exactly one store5 settings row is required before migration.', 1;

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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.DealerSystemSettingExtension') AND is_primary_key = 1)
    THROW 51004, 'The extension table is missing its primary key.', 1;

PRINT 'DealerSystemSettingExtension is ready in db82141 for store5.';
