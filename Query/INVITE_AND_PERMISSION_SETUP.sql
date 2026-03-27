USE [MOM];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.MST_UserInvite', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MST_UserInvite
    (
        UserInviteID INT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NOT NULL,
        InviteEmail NVARCHAR(160) NOT NULL,
        InviteToken NVARCHAR(140) NOT NULL UNIQUE,
        ExpiresAt DATETIME NOT NULL,
        IsUsed BIT NOT NULL CONSTRAINT DF_MST_UserInvite_IsUsed DEFAULT(0),
        UsedAt DATETIME NULL,
        DeliveryStatus NVARCHAR(40) NULL,
        DeliveryChannel NVARCHAR(40) NULL,
        PreviewPath NVARCHAR(400) NULL,
        SentAt DATETIME NULL,
        Created DATETIME NOT NULL CONSTRAINT DF_MST_UserInvite_Created DEFAULT(GETDATE()),
        Modified DATETIME NOT NULL CONSTRAINT DF_MST_UserInvite_Modified DEFAULT(GETDATE()),
        CONSTRAINT FK_MST_UserInvite_User FOREIGN KEY (UserID) REFERENCES dbo.MST_User(UserID)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MST_UserInvite_UserID_IsUsed'
      AND object_id = OBJECT_ID('dbo.MST_UserInvite')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MST_UserInvite_UserID_IsUsed
    ON dbo.MST_UserInvite(UserID, IsUsed, ExpiresAt DESC);
END
GO

MERGE dbo.MST_SystemSetting AS target
USING (VALUES
    ('SmtpHost', '', 'Email', 'SMTP host for invite delivery'),
    ('SmtpPort', '587', 'Email', 'SMTP port for invite delivery'),
    ('SmtpUsername', '', 'Email', 'SMTP username'),
    ('SmtpPassword', '', 'Email', 'SMTP password'),
    ('SmtpFromEmail', '', 'Email', 'SMTP sender email'),
    ('SmtpFromName', 'Meeting Of Minutes', 'Email', 'SMTP sender display name'),
    ('SmtpUseSsl', 'True', 'Email', 'SMTP SSL flag'),
    ('InviteBaseUrl', 'http://localhost:7289', 'Email', 'Base URL used for onboarding invite links')
) AS source(SettingKey, SettingValue, SettingGroup, Description)
ON target.SettingKey = source.SettingKey
WHEN MATCHED THEN
    UPDATE SET SettingGroup = source.SettingGroup, Description = source.Description, Modified = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (SettingKey, SettingValue, SettingGroup, Description, Modified)
    VALUES (source.SettingKey, source.SettingValue, source.SettingGroup, source.Description, GETDATE());
GO
