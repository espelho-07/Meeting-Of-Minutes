USE [MOM];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.MST_User', 'ManagedByAdminUserID') IS NULL
BEGIN
    ALTER TABLE dbo.MST_User
    ADD ManagedByAdminUserID INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_MST_User_ManagedByAdminUserID'
)
BEGIN
    ALTER TABLE dbo.MST_User WITH CHECK
    ADD CONSTRAINT FK_MST_User_ManagedByAdminUserID
        FOREIGN KEY (ManagedByAdminUserID) REFERENCES dbo.MST_User(UserID);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MST_User_ManagedByAdminUserID'
      AND object_id = OBJECT_ID('dbo.MST_User')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MST_User_ManagedByAdminUserID
    ON dbo.MST_User(ManagedByAdminUserID, CompanyName, UserRole, IsActive);
END
GO

DECLARE @SuperAdminUserID INT;
DECLARE @MeetAdminUserID INT;
DECLARE @BhargavAdminUserID INT;
DECLARE @DeepAdminUserID INT;
DECLARE @HetUserID INT;
DECLARE @VivekUserID INT;
DECLARE @KaranUserID INT;

SELECT @SuperAdminUserID = UserID FROM dbo.MST_User WHERE Email = 'darpanparmar1707@gmail.com';
SELECT @MeetAdminUserID = UserID FROM dbo.MST_User WHERE Email = 'meet01@darshan.ac.in';
SELECT @BhargavAdminUserID = UserID FROM dbo.MST_User WHERE Email = 'bhargav01@marwadi.ac.in';
SELECT @DeepAdminUserID = UserID FROM dbo.MST_User WHERE Email = 'Deep02@gmail.com';
SELECT @HetUserID = UserID FROM dbo.MST_User WHERE Email = 'het01@darshan.ac.in';
SELECT @VivekUserID = UserID FROM dbo.MST_User WHERE Email = 'vivek01@darshan.ac.in';
SELECT @KaranUserID = UserID FROM dbo.MST_User WHERE Email = 'karan02@darshan.ac.in';

UPDATE dbo.MST_User
SET UserRole = 'SuperAdmin',
    ManagedByAdminUserID = NULL,
    Modified = GETDATE()
WHERE UserID = @SuperAdminUserID;

UPDATE dbo.MST_User
SET UserRole = 'Admin',
    ManagedByAdminUserID = NULL,
    Modified = GETDATE()
WHERE UserID IN (@MeetAdminUserID, @BhargavAdminUserID, @DeepAdminUserID);

UPDATE dbo.MST_User
SET ManagedByAdminUserID = @MeetAdminUserID,
    Modified = GETDATE()
WHERE UserID IN (@HetUserID, @VivekUserID)
  AND UserRole = 'User';

UPDATE dbo.MST_User
SET ManagedByAdminUserID = @DeepAdminUserID,
    Modified = GETDATE()
WHERE UserID = @KaranUserID
  AND UserRole = 'User';

UPDATE dbo.MST_User
SET ManagedByAdminUserID = NULL,
    Modified = GETDATE()
WHERE UserRole IN ('SuperAdmin', 'Admin');
GO
