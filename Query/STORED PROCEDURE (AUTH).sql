USE [MOM];
GO

IF OBJECT_ID('dbo.MST_User', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MST_User]
    (
        [UserID] INT IDENTITY(1,1) PRIMARY KEY,
        [UserName] NVARCHAR(50) NOT NULL UNIQUE,
        [Email] NVARCHAR(150) NOT NULL UNIQUE,
        [Password] NVARCHAR(50) NOT NULL,
        [ContactNo] NVARCHAR(15) NULL,
        [City] NVARCHAR(100) NULL,
        [UserRole] NVARCHAR(20) NOT NULL CONSTRAINT DF_MST_User_UserRole DEFAULT('User'),
        [CompanyName] NVARCHAR(100) NULL,
        [StaffID] INT NULL,
        [DepartmentID] INT NULL,
        [IsAutoPassword] BIT NOT NULL CONSTRAINT DF_MST_User_IsAutoPassword DEFAULT(0),
        [IsActive] BIT NOT NULL CONSTRAINT DF_MST_User_IsActive DEFAULT(1),
        [Created] DATETIME NOT NULL CONSTRAINT DF_MST_User_Created DEFAULT(GETDATE()),
        [Modified] DATETIME NOT NULL
    );
END
GO

IF COL_LENGTH('dbo.MST_User', 'Email') IS NULL
BEGIN
    ALTER TABLE [dbo].[MST_User] ADD [Email] NVARCHAR(150) NULL;
END
GO

IF COL_LENGTH('dbo.MST_User', 'ContactNo') IS NULL
BEGIN
    ALTER TABLE [dbo].[MST_User] ADD [ContactNo] NVARCHAR(15) NULL;
END
GO

IF COL_LENGTH('dbo.MST_User', 'City') IS NULL
BEGIN
    ALTER TABLE [dbo].[MST_User] ADD [City] NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.MST_User', 'UserRole') IS NULL
BEGIN
    ALTER TABLE [dbo].[MST_User] ADD [UserRole] NVARCHAR(20) NOT NULL CONSTRAINT DF_MST_User_Role DEFAULT('User');
END
GO

IF COL_LENGTH('dbo.MST_User', 'CompanyName') IS NULL
BEGIN
    ALTER TABLE [dbo].[MST_User] ADD [CompanyName] NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.MST_User', 'StaffID') IS NULL
BEGIN
    ALTER TABLE [dbo].[MST_User] ADD [StaffID] INT NULL;
END
GO

IF COL_LENGTH('dbo.MST_User', 'DepartmentID') IS NULL
BEGIN
    ALTER TABLE [dbo].[MST_User] ADD [DepartmentID] INT NULL;
END
GO

IF COL_LENGTH('dbo.MST_User', 'IsAutoPassword') IS NULL
BEGIN
    ALTER TABLE [dbo].[MST_User] ADD [IsAutoPassword] BIT NOT NULL CONSTRAINT DF_MST_User_AutoPassword DEFAULT(0);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_MST_User_Email' AND object_id = OBJECT_ID('dbo.MST_User'))
BEGIN
    CREATE UNIQUE INDEX UX_MST_User_Email ON dbo.MST_User(Email) WHERE Email IS NOT NULL;
END
GO

UPDATE dbo.MST_User
SET
    Email = ISNULL(NULLIF(Email, ''), UserName + '@mom.local'),
    ContactNo = ISNULL(ContactNo, ''),
    City = ISNULL(City, ''),
    UserRole = ISNULL(NULLIF(UserRole, ''), 'Admin'),
    CompanyName = ISNULL(NULLIF(CompanyName, ''), 'Default Company'),
    IsAutoPassword = ISNULL(IsAutoPassword, 0)
WHERE Email IS NULL
   OR ContactNo IS NULL
   OR City IS NULL
   OR UserRole IS NULL
   OR UserRole = ''
   OR CompanyName IS NULL
   OR CompanyName = '';
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_Company_DDL]
AS
BEGIN
    SELECT DISTINCT CompanyName
    FROM dbo.MST_User
    WHERE CompanyName IS NOT NULL AND CompanyName <> ''
    ORDER BY CompanyName;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_User_SelectForLogin]
    @Email NVARCHAR(150),
    @Password NVARCHAR(50),
    @UserRole NVARCHAR(20),
    @CompanyName NVARCHAR(100),
    @DepartmentID INT = NULL
AS
BEGIN
    SELECT
        u.[UserID],
        u.[UserName],
        u.[Email],
        u.[Password],
        u.[ContactNo],
        u.[City],
        u.[UserRole],
        u.[CompanyName],
        u.[StaffID],
        u.[DepartmentID],
        u.[IsAutoPassword],
        ISNULL(d.[DepartmentName], '') AS [DepartmentName],
        u.[IsActive],
        u.[Created],
        u.[Modified]
    FROM [dbo].[MST_User] u
    LEFT JOIN [dbo].[MOM_Department] d
        ON d.[DepartmentID] = u.[DepartmentID]
    WHERE u.[Email] = @Email
      AND u.[Password] = @Password
      AND u.[UserRole] = @UserRole
      AND u.[CompanyName] = @CompanyName
      AND u.[IsActive] = 1
      AND (@UserRole <> 'User' OR u.[DepartmentID] = @DepartmentID);
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_User_SelectByEmail]
    @Email NVARCHAR(150)
AS
BEGIN
    SELECT
        u.[UserID],
        u.[UserName],
        u.[Email],
        u.[ContactNo],
        u.[City],
        u.[UserRole],
        u.[CompanyName],
        u.[StaffID],
        u.[DepartmentID],
        u.[IsAutoPassword],
        ISNULL(d.[DepartmentName], '') AS [DepartmentName],
        u.[IsActive],
        u.[Created],
        u.[Modified]
    FROM [dbo].[MST_User] u
    LEFT JOIN [dbo].[MOM_Department] d
        ON d.[DepartmentID] = u.[DepartmentID]
    WHERE u.[Email] = @Email;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_User_SelectByPK]
    @UserID INT
AS
BEGIN
    SELECT
        u.[UserID],
        u.[UserName],
        u.[Email],
        u.[ContactNo],
        u.[City],
        u.[UserRole],
        u.[CompanyName],
        u.[StaffID],
        u.[DepartmentID],
        u.[IsAutoPassword],
        ISNULL(d.[DepartmentName], '') AS [DepartmentName],
        u.[IsActive],
        u.[Created],
        u.[Modified]
    FROM [dbo].[MST_User] u
    LEFT JOIN [dbo].[MOM_Department] d
        ON d.[DepartmentID] = u.[DepartmentID]
    WHERE u.[UserID] = @UserID;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_User_Insert]
    @UserName NVARCHAR(50),
    @Email NVARCHAR(150),
    @Password NVARCHAR(50),
    @ContactNo NVARCHAR(15),
    @City NVARCHAR(100),
    @UserRole NVARCHAR(20),
    @CompanyName NVARCHAR(100),
    @StaffID INT = NULL,
    @DepartmentID INT = NULL,
    @IsActive BIT,
    @Modified DATETIME
AS
BEGIN
    INSERT INTO [dbo].[MST_User]
    (
        [UserName],
        [Email],
        [Password],
        [ContactNo],
        [City],
        [UserRole],
        [CompanyName],
        [StaffID],
        [DepartmentID],
        [IsAutoPassword],
        [IsActive],
        [Created],
        [Modified]
    )
    VALUES
    (
        @UserName,
        @Email,
        @Password,
        @ContactNo,
        @City,
        @UserRole,
        @CompanyName,
        @StaffID,
        @DepartmentID,
        0,
        @IsActive,
        GETDATE(),
        @Modified
    );
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_User_UpsertForStaff]
    @StaffID INT,
    @UserName NVARCHAR(50),
    @Email NVARCHAR(150),
    @Password NVARCHAR(50),
    @ContactNo NVARCHAR(15),
    @City NVARCHAR(100),
    @CompanyName NVARCHAR(100),
    @DepartmentID INT,
    @Modified DATETIME
AS
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.MST_User WHERE StaffID = @StaffID)
    BEGIN
        UPDATE dbo.MST_User
        SET
            UserName = @UserName,
            Email = @Email,
            Password = CASE WHEN IsAutoPassword = 1 THEN @Password ELSE Password END,
            ContactNo = @ContactNo,
            City = @City,
            UserRole = 'User',
            CompanyName = @CompanyName,
            DepartmentID = @DepartmentID,
            IsActive = 1,
            Modified = @Modified
        WHERE StaffID = @StaffID;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.MST_User
        (
            UserName,
            Email,
            Password,
            ContactNo,
            City,
            UserRole,
            CompanyName,
            StaffID,
            DepartmentID,
            IsAutoPassword,
            IsActive,
            Created,
            Modified
        )
        VALUES
        (
            @UserName,
            @Email,
            @Password,
            @ContactNo,
            @City,
            'User',
            @CompanyName,
            @StaffID,
            @DepartmentID,
            1,
            1,
            GETDATE(),
            @Modified
        );
    END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_User_SelectByStaffID]
    @StaffID INT
AS
BEGIN
    SELECT TOP 1
        UserID,
        UserName,
        Email,
        Password,
        ContactNo,
        City,
        UserRole,
        CompanyName,
        StaffID,
        DepartmentID,
        IsAutoPassword,
        IsActive,
        Created,
        Modified
    FROM dbo.MST_User
    WHERE StaffID = @StaffID;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_User_UpdatePasswordByPK]
    @UserID INT,
    @NewPassword NVARCHAR(50)
AS
BEGIN
    UPDATE dbo.MST_User
    SET
        Password = @NewPassword,
        IsAutoPassword = 0,
        Modified = GETDATE()
    WHERE UserID = @UserID;
END
GO

IF OBJECT_ID('dbo.MST_ProfileUpdateRequest', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MST_ProfileUpdateRequest]
    (
        [ProfileUpdateRequestID] INT IDENTITY(1,1) PRIMARY KEY,
        [UserID] INT NOT NULL,
        [StaffID] INT NULL,
        [RequestedUserName] NVARCHAR(50) NOT NULL,
        [RequestedEmail] NVARCHAR(150) NOT NULL,
        [RequestedContactNo] NVARCHAR(15) NULL,
        [RequestedCity] NVARCHAR(100) NULL,
        [DocumentPath] NVARCHAR(300) NOT NULL,
        [RequestStatus] NVARCHAR(20) NOT NULL CONSTRAINT DF_MST_ProfileUpdateRequest_Status DEFAULT('Pending'),
        [AdminRemarks] NVARCHAR(250) NULL,
        [Created] DATETIME NOT NULL CONSTRAINT DF_MST_ProfileUpdateRequest_Created DEFAULT(GETDATE()),
        [Modified] DATETIME NOT NULL,
        [ApprovedRejectedByUserID] INT NULL,
        [DecisionDate] DATETIME NULL
    );
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_ProfileUpdateRequest_Insert]
    @UserID INT,
    @StaffID INT = NULL,
    @RequestedUserName NVARCHAR(50),
    @RequestedEmail NVARCHAR(150),
    @RequestedContactNo NVARCHAR(15),
    @RequestedCity NVARCHAR(100),
    @DocumentPath NVARCHAR(300),
    @Modified DATETIME
AS
BEGIN
    INSERT INTO dbo.MST_ProfileUpdateRequest
    (
        UserID,
        StaffID,
        RequestedUserName,
        RequestedEmail,
        RequestedContactNo,
        RequestedCity,
        DocumentPath,
        RequestStatus,
        Modified
    )
    VALUES
    (
        @UserID,
        @StaffID,
        @RequestedUserName,
        @RequestedEmail,
        @RequestedContactNo,
        @RequestedCity,
        @DocumentPath,
        'Pending',
        @Modified
    );
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_ProfileUpdateRequest_SelectByUserID]
    @UserID INT
AS
BEGIN
    SELECT
        r.ProfileUpdateRequestID,
        r.UserID,
        r.StaffID,
        u.UserName AS CurrentUserName,
        u.Email AS CurrentEmail,
        u.ContactNo AS CurrentContactNo,
        u.City AS CurrentCity,
        r.RequestedUserName,
        r.RequestedEmail,
        r.RequestedContactNo,
        r.RequestedCity,
        r.DocumentPath,
        r.RequestStatus,
        r.AdminRemarks,
        r.Created,
        r.Modified,
        r.ApprovedRejectedByUserID,
        r.DecisionDate
    FROM dbo.MST_ProfileUpdateRequest r
    INNER JOIN dbo.MST_User u ON u.UserID = r.UserID
    WHERE r.UserID = @UserID
    ORDER BY r.ProfileUpdateRequestID DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_ProfileUpdateRequest_SelectAll]
    @CompanyName NVARCHAR(100) = NULL
AS
BEGIN
    SELECT
        r.ProfileUpdateRequestID,
        r.UserID,
        r.StaffID,
        u.UserName AS CurrentUserName,
        u.Email AS CurrentEmail,
        u.ContactNo AS CurrentContactNo,
        u.City AS CurrentCity,
        u.CompanyName,
        ISNULL(d.DepartmentName, '') AS DepartmentName,
        r.RequestedUserName,
        r.RequestedEmail,
        r.RequestedContactNo,
        r.RequestedCity,
        r.DocumentPath,
        r.RequestStatus,
        r.AdminRemarks,
        r.Created,
        r.Modified,
        r.ApprovedRejectedByUserID,
        r.DecisionDate
    FROM dbo.MST_ProfileUpdateRequest r
    INNER JOIN dbo.MST_User u ON u.UserID = r.UserID
    LEFT JOIN dbo.MOM_Department d ON d.DepartmentID = u.DepartmentID
    WHERE @CompanyName IS NULL OR u.CompanyName = @CompanyName
    ORDER BY
        CASE WHEN r.RequestStatus = 'Pending' THEN 0 ELSE 1 END,
        r.ProfileUpdateRequestID DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_ProfileUpdateRequest_Approve]
    @ProfileUpdateRequestID INT,
    @AdminUserID INT,
    @AdminRemarks NVARCHAR(250) = NULL
AS
BEGIN
    DECLARE @UserID INT;
    DECLARE @StaffID INT;
    DECLARE @RequestedUserName NVARCHAR(50);
    DECLARE @RequestedEmail NVARCHAR(150);
    DECLARE @RequestedContactNo NVARCHAR(15);
    DECLARE @RequestedCity NVARCHAR(100);

    SELECT
        @UserID = UserID,
        @StaffID = StaffID,
        @RequestedUserName = RequestedUserName,
        @RequestedEmail = RequestedEmail,
        @RequestedContactNo = RequestedContactNo,
        @RequestedCity = RequestedCity
    FROM dbo.MST_ProfileUpdateRequest
    WHERE ProfileUpdateRequestID = @ProfileUpdateRequestID
      AND RequestStatus = 'Pending';

    IF @UserID IS NULL
    BEGIN
        RAISERROR('Pending request not found.', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1
        FROM dbo.MST_User
        WHERE Email = @RequestedEmail
          AND UserID <> @UserID
    )
    BEGIN
        RAISERROR('Requested email already exists.', 16, 1);
        RETURN;
    END

    IF @StaffID IS NOT NULL
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM dbo.MOM_Staff
            WHERE EmailAddress = @RequestedEmail
              AND StaffID <> @StaffID
        )
        BEGIN
            RAISERROR('Requested staff email already exists.', 16, 1);
            RETURN;
        END
    END

    UPDATE dbo.MST_User
    SET
        UserName = @RequestedUserName,
        Email = @RequestedEmail,
        ContactNo = @RequestedContactNo,
        City = @RequestedCity,
        Modified = GETDATE()
    WHERE UserID = @UserID;

    IF @StaffID IS NOT NULL
    BEGIN
        UPDATE dbo.MOM_Staff
        SET
            StaffName = @RequestedUserName,
            EmailAddress = @RequestedEmail,
            MobileNo = @RequestedContactNo
        WHERE StaffID = @StaffID;
    END

    UPDATE dbo.MST_ProfileUpdateRequest
    SET
        RequestStatus = 'Approved',
        AdminRemarks = @AdminRemarks,
        Modified = GETDATE(),
        ApprovedRejectedByUserID = @AdminUserID,
        DecisionDate = GETDATE()
    WHERE ProfileUpdateRequestID = @ProfileUpdateRequestID;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_ProfileUpdateRequest_Reject]
    @ProfileUpdateRequestID INT,
    @AdminUserID INT,
    @AdminRemarks NVARCHAR(250) = NULL
AS
BEGIN
    UPDATE dbo.MST_ProfileUpdateRequest
    SET
        RequestStatus = 'Rejected',
        AdminRemarks = @AdminRemarks,
        Modified = GETDATE(),
        ApprovedRejectedByUserID = @AdminUserID,
        DecisionDate = GETDATE()
    WHERE ProfileUpdateRequestID = @ProfileUpdateRequestID
      AND RequestStatus = 'Pending';
END
GO

IF OBJECT_ID('dbo.MST_AdminNotification', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MST_AdminNotification]
    (
        [AdminNotificationID] INT IDENTITY(1,1) PRIMARY KEY,
        [CompanyName] NVARCHAR(100) NULL,
        [NotificationType] NVARCHAR(50) NOT NULL,
        [Title] NVARCHAR(150) NOT NULL,
        [Message] NVARCHAR(300) NOT NULL,
        [RelatedUserID] INT NULL,
        [IsRead] BIT NOT NULL CONSTRAINT DF_MST_AdminNotification_IsRead DEFAULT(0),
        [Created] DATETIME NOT NULL CONSTRAINT DF_MST_AdminNotification_Created DEFAULT(GETDATE()),
        [Modified] DATETIME NOT NULL
    );
END
GO

IF COL_LENGTH('dbo.MST_AdminNotification', 'IsRead') IS NULL
BEGIN
    ALTER TABLE [dbo].[MST_AdminNotification] ADD [IsRead] BIT NOT NULL CONSTRAINT DF_MST_AdminNotification_IsRead_Legacy DEFAULT(0);
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_AdminNotification_Insert]
    @CompanyName NVARCHAR(100) = NULL,
    @NotificationType NVARCHAR(50),
    @Title NVARCHAR(150),
    @Message NVARCHAR(300),
    @RelatedUserID INT = NULL,
    @Modified DATETIME
AS
BEGIN
    INSERT INTO dbo.MST_AdminNotification
    (
        CompanyName,
        NotificationType,
        Title,
        Message,
        RelatedUserID,
        Modified
    )
    VALUES
    (
        @CompanyName,
        @NotificationType,
        @Title,
        @Message,
        @RelatedUserID,
        @Modified
    );
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_AdminNotification_SelectAll]
    @CompanyName NVARCHAR(100) = NULL
AS
BEGIN
    SELECT TOP 12
        AdminNotificationID,
        CompanyName,
        NotificationType,
        Title,
        Message,
        RelatedUserID,
        IsRead,
        Created
    FROM dbo.MST_AdminNotification
    WHERE @CompanyName IS NULL OR CompanyName = @CompanyName
    ORDER BY AdminNotificationID DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_AdminNotification_MarkAllRead]
    @CompanyName NVARCHAR(100) = NULL
AS
BEGIN
    UPDATE dbo.MST_AdminNotification
    SET
        IsRead = 1,
        Modified = GETDATE()
    WHERE (@CompanyName IS NULL OR CompanyName = @CompanyName)
      AND IsRead = 0;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_AdminNotification_ClearAll]
    @CompanyName NVARCHAR(100) = NULL
AS
BEGIN
    DELETE FROM dbo.MST_AdminNotification
    WHERE @CompanyName IS NULL OR CompanyName = @CompanyName;
END
GO

IF OBJECT_ID('dbo.MST_StaffTransferRequest', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MST_StaffTransferRequest]
    (
        [StaffTransferRequestID] INT IDENTITY(1,1) PRIMARY KEY,
        [StaffID] INT NOT NULL,
        [UserID] INT NULL,
        [SourceCompanyName] NVARCHAR(100) NOT NULL,
        [SourceDepartmentID] INT NOT NULL,
        [TargetCompanyName] NVARCHAR(100) NOT NULL,
        [TargetDepartmentID] INT NOT NULL,
        [RequestedByUserID] INT NOT NULL,
        [TransferReason] NVARCHAR(500) NULL,
        [DocumentPath] NVARCHAR(300) NOT NULL,
        [RequestStatus] NVARCHAR(20) NOT NULL CONSTRAINT DF_MST_StaffTransferRequest_Status DEFAULT('Pending'),
        [AdminRemarks] NVARCHAR(250) NULL,
        [Created] DATETIME NOT NULL CONSTRAINT DF_MST_StaffTransferRequest_Created DEFAULT(GETDATE()),
        [Modified] DATETIME NOT NULL,
        [ApprovedRejectedByUserID] INT NULL,
        [DecisionDate] DATETIME NULL
    );
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_StaffTransferRequest_Insert]
    @StaffID INT,
    @UserID INT = NULL,
    @SourceCompanyName NVARCHAR(100),
    @SourceDepartmentID INT,
    @TargetCompanyName NVARCHAR(100),
    @TargetDepartmentID INT,
    @RequestedByUserID INT,
    @TransferReason NVARCHAR(500),
    @DocumentPath NVARCHAR(300),
    @Modified DATETIME
AS
BEGIN
    IF EXISTS (
        SELECT 1
        FROM dbo.MST_StaffTransferRequest
        WHERE StaffID = @StaffID
          AND RequestStatus = 'Pending'
    )
    BEGIN
        RAISERROR('A pending transfer request already exists for this staff member.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.MST_StaffTransferRequest
    (
        StaffID,
        UserID,
        SourceCompanyName,
        SourceDepartmentID,
        TargetCompanyName,
        TargetDepartmentID,
        RequestedByUserID,
        TransferReason,
        DocumentPath,
        RequestStatus,
        Modified
    )
    VALUES
    (
        @StaffID,
        @UserID,
        @SourceCompanyName,
        @SourceDepartmentID,
        @TargetCompanyName,
        @TargetDepartmentID,
        @RequestedByUserID,
        @TransferReason,
        @DocumentPath,
        'Pending',
        @Modified
    );
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_StaffTransferRequest_SelectByStaffID]
    @StaffID INT
AS
BEGIN
    SELECT
        r.StaffTransferRequestID,
        r.StaffID,
        r.UserID,
        s.StaffName,
        s.EmailAddress,
        s.MobileNo,
        r.SourceCompanyName,
        r.SourceDepartmentID,
        ISNULL(sd.DepartmentName, '') AS SourceDepartmentName,
        r.TargetCompanyName,
        r.TargetDepartmentID,
        ISNULL(td.DepartmentName, '') AS TargetDepartmentName,
        r.RequestedByUserID,
        ISNULL(ru.UserName, '') AS RequestedByUserName,
        r.TransferReason,
        r.DocumentPath,
        r.RequestStatus,
        r.AdminRemarks,
        r.Created,
        r.DecisionDate
    FROM dbo.MST_StaffTransferRequest r
    INNER JOIN dbo.MOM_Staff s ON s.StaffID = r.StaffID
    LEFT JOIN dbo.MOM_Department sd ON sd.DepartmentID = r.SourceDepartmentID
    LEFT JOIN dbo.MOM_Department td ON td.DepartmentID = r.TargetDepartmentID
    LEFT JOIN dbo.MST_User ru ON ru.UserID = r.RequestedByUserID
    WHERE r.StaffID = @StaffID
    ORDER BY r.StaffTransferRequestID DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_StaffTransferRequest_SelectIncoming]
    @TargetCompanyName NVARCHAR(100)
AS
BEGIN
    SELECT
        r.StaffTransferRequestID,
        r.StaffID,
        r.UserID,
        s.StaffName,
        s.EmailAddress,
        s.MobileNo,
        r.SourceCompanyName,
        r.SourceDepartmentID,
        ISNULL(sd.DepartmentName, '') AS SourceDepartmentName,
        r.TargetCompanyName,
        r.TargetDepartmentID,
        ISNULL(td.DepartmentName, '') AS TargetDepartmentName,
        r.RequestedByUserID,
        ISNULL(ru.UserName, '') AS RequestedByUserName,
        r.TransferReason,
        r.DocumentPath,
        r.RequestStatus,
        r.AdminRemarks,
        r.Created,
        r.DecisionDate
    FROM dbo.MST_StaffTransferRequest r
    INNER JOIN dbo.MOM_Staff s ON s.StaffID = r.StaffID
    LEFT JOIN dbo.MOM_Department sd ON sd.DepartmentID = r.SourceDepartmentID
    LEFT JOIN dbo.MOM_Department td ON td.DepartmentID = r.TargetDepartmentID
    LEFT JOIN dbo.MST_User ru ON ru.UserID = r.RequestedByUserID
    WHERE r.TargetCompanyName = @TargetCompanyName
    ORDER BY
        CASE WHEN r.RequestStatus = 'Pending' THEN 0 ELSE 1 END,
        r.StaffTransferRequestID DESC;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_StaffTransferRequest_Approve]
    @StaffTransferRequestID INT,
    @AdminUserID INT,
    @AdminRemarks NVARCHAR(250) = NULL
AS
BEGIN
    DECLARE @StaffID INT;
    DECLARE @UserID INT;
    DECLARE @TargetCompanyName NVARCHAR(100);
    DECLARE @TargetDepartmentID INT;

    SELECT
        @StaffID = StaffID,
        @UserID = UserID,
        @TargetCompanyName = TargetCompanyName,
        @TargetDepartmentID = TargetDepartmentID
    FROM dbo.MST_StaffTransferRequest
    WHERE StaffTransferRequestID = @StaffTransferRequestID
      AND RequestStatus = 'Pending';

    IF @StaffID IS NULL
    BEGIN
        RAISERROR('Pending transfer request not found.', 16, 1);
        RETURN;
    END

    UPDATE dbo.MOM_Staff
    SET DepartmentID = @TargetDepartmentID
    WHERE StaffID = @StaffID;

    UPDATE dbo.MST_User
    SET
        CompanyName = @TargetCompanyName,
        DepartmentID = @TargetDepartmentID,
        Modified = GETDATE()
    WHERE StaffID = @StaffID;

    DELETE FROM dbo.MOM_MeetingMember
    WHERE StaffID = @StaffID;

    UPDATE dbo.MST_StaffTransferRequest
    SET
        RequestStatus = 'Approved',
        AdminRemarks = @AdminRemarks,
        Modified = GETDATE(),
        ApprovedRejectedByUserID = @AdminUserID,
        DecisionDate = GETDATE()
    WHERE StaffTransferRequestID = @StaffTransferRequestID;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[PR_MST_StaffTransferRequest_Reject]
    @StaffTransferRequestID INT,
    @AdminUserID INT,
    @AdminRemarks NVARCHAR(250) = NULL
AS
BEGIN
    UPDATE dbo.MST_StaffTransferRequest
    SET
        RequestStatus = 'Rejected',
        AdminRemarks = @AdminRemarks,
        Modified = GETDATE(),
        ApprovedRejectedByUserID = @AdminUserID,
        DecisionDate = GETDATE()
    WHERE StaffTransferRequestID = @StaffTransferRequestID
      AND RequestStatus = 'Pending';
END
GO
