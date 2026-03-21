GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MST_User_SelectForLogin]
    @UserName nvarchar(50),
    @Password nvarchar(50)
AS
BEGIN
    SELECT
        MST_User.UserID,
        MST_User.UserName,
        MST_User.[Password],
        MST_User.IsActive,
        MST_User.Created,
        MST_User.Modified
    FROM MST_User
    WHERE [UserName] = @UserName
      AND [Password] = @Password
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_User_Insert]
    @UserName nvarchar(50),
    @Password nvarchar(50),
    @IsActive bit,
    @Modified datetime
AS
BEGIN
    INSERT INTO MST_User
    (
        UserName,
        [Password],
        IsActive,
        Created,
        Modified
    )
    VALUES
    (
        @UserName,
        @Password,
        @IsActive,
        GETDATE(),
        @Modified
    )
END
GO

USE [MOM];
GO

IF OBJECT_ID('dbo.MST_User', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MST_User]
    (
        [UserID]   INT IDENTITY(1,1) PRIMARY KEY,
        [UserName] NVARCHAR(50) NOT NULL UNIQUE,
        [Password] NVARCHAR(50) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT DF_MST_User_IsActive DEFAULT(1),
        [Created]  DATETIME NOT NULL CONSTRAINT DF_MST_User_Created DEFAULT(GETDATE()),
        [Modified] DATETIME NOT NULL
    );
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_User_SelectForLogin]
    @UserName NVARCHAR(50),
    @Password NVARCHAR(50)
AS
BEGIN
    SELECT
        [UserID],
        [UserName],
        [Password],
        [IsActive],
        [Created],
        [Modified]
    FROM [dbo].[MST_User]
    WHERE [UserName] = @UserName
      AND [Password] = @Password;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_User_Insert]
    @UserName NVARCHAR(50),
    @Password NVARCHAR(50),
    @IsActive BIT,
    @Modified DATETIME
AS
BEGIN
    INSERT INTO [dbo].[MST_User]
    (
        [UserName],
        [Password],
        [IsActive],
        [Created],
        [Modified]
    )
    VALUES
    (
        @UserName,
        @Password,
        @IsActive,
        GETDATE(),
        @Modified
    );
END
GO
