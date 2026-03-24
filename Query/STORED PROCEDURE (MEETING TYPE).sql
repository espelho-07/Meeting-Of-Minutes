-- =============================================
-- STORED PROCEDURES FOR MEETING TYPE
-- TABLE : [dbo].[MOM_MeetingType]
-- =============================================

IF COL_LENGTH('dbo.MOM_MeetingType', 'CompanyName') IS NULL
BEGIN
    ALTER TABLE [dbo].[MOM_MeetingType]
    ADD [CompanyName] NVARCHAR(100) NULL;
END;
GO

UPDATE mt
SET mt.CompanyName = seed.CompanyName
FROM [dbo].[MOM_MeetingType] mt
CROSS APPLY
(
    SELECT TOP 1 [CompanyName]
    FROM [dbo].[MST_User]
    WHERE [UserRole] = 'Admin'
      AND [CompanyName] IS NOT NULL
    ORDER BY [UserID]
) seed
WHERE mt.CompanyName IS NULL;
GO

-- 1 -> SelectAll Procedure [For List Page]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingType_SelectAll]
(
    @CompanyName NVARCHAR(100),
    @searchtext NVARCHAR(100) = NULL
)
AS
BEGIN
    SELECT
        [dbo].[MOM_MeetingType].[MeetingTypeID],
        [dbo].[MOM_MeetingType].[MeetingTypeName],
        [dbo].[MOM_MeetingType].[Remarks],
        [dbo].[MOM_MeetingType].[Created],
        [dbo].[MOM_MeetingType].[Modified]
    FROM [dbo].[MOM_MeetingType]
    WHERE [dbo].[MOM_MeetingType].[CompanyName] = @CompanyName
      AND (
           @searchtext IS NULL
        OR [dbo].[MOM_MeetingType].[MeetingTypeName] LIKE '%' + @searchtext + '%'
        OR [dbo].[MOM_MeetingType].[Remarks] LIKE '%' + @searchtext + '%'
      )
    ORDER BY [dbo].[MOM_MeetingType].[MeetingTypeID] DESC;
END;
GO

-- 2 -> SelectByPK Procedure [Edit time record fetch & fill controls]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingType_SelectByPK]
(
    @MeetingTypeID INT,
    @CompanyName NVARCHAR(100)
)
AS
BEGIN
    SELECT
        [dbo].[MOM_MeetingType].[MeetingTypeID],
        [dbo].[MOM_MeetingType].[MeetingTypeName],
        [dbo].[MOM_MeetingType].[Remarks],
        [dbo].[MOM_MeetingType].[Created],
        [dbo].[MOM_MeetingType].[Modified]
    FROM [dbo].[MOM_MeetingType]
    WHERE [dbo].[MOM_MeetingType].[MeetingTypeID] = @MeetingTypeID
      AND [dbo].[MOM_MeetingType].[CompanyName] = @CompanyName;
END;
GO

-- 3 -> Insert Procedure [To add any new record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingType_Insert]
(
    @MeetingTypeName NVARCHAR(100),
    @Remarks         NVARCHAR(100),
    @CompanyName     NVARCHAR(100),
    @Modified        DATETIME
)
AS
BEGIN
    INSERT INTO [dbo].[MOM_MeetingType]
    (
        [MeetingTypeName],
        [Remarks],
        [CompanyName],
        [Created],
        [Modified]
    )
    VALUES
    (
        @MeetingTypeName,
        @Remarks,
        @CompanyName,
        GETDATE(),
        @Modified
    );
END;
GO

-- 4 -> UpdateByPK Procedure [To update/modify existing record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingType_UpdateByPK]
(
    @MeetingTypeID   INT,
    @MeetingTypeName NVARCHAR(100),
    @Remarks         NVARCHAR(100),
    @CompanyName     NVARCHAR(100)
)
AS
BEGIN
    UPDATE [dbo].[MOM_MeetingType]
    SET
        [MeetingTypeName] = @MeetingTypeName,
        [Remarks]         = @Remarks,
        [Modified]        = GETDATE()
    WHERE [MeetingTypeID] = @MeetingTypeID
      AND [CompanyName] = @CompanyName;
END;
GO

-- 5 -> DeleteByPK Procedure [To delete record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingType_DeleteByPK]
(
    @MeetingTypeID INT
)
AS
BEGIN
    DELETE
    FROM [dbo].[MOM_MeetingType]
    WHERE [dbo].[MOM_MeetingType].[MeetingTypeID] = @MeetingTypeID;
END;
GO


INSERT INTO [dbo].[MOM_MeetingType]
(
    MeetingTypeName,
    Remarks,
    CompanyName,
    Created,
    Modified
)
VALUES
('Internal', 'Internal department meeting', 'Darshan University', GETDATE(), GETDATE()),
('External', 'Meeting with external clients', 'Darshan University', GETDATE(), GETDATE()),
('Review', 'Quarterly review meeting', 'Darshan University', GETDATE(), GETDATE()),
('Planning', 'Strategic planning discussion', 'Darshan University', GETDATE(), GETDATE()),
('Emergency', 'Urgent issue handling meeting', 'Darshan University', GETDATE(), GETDATE());
