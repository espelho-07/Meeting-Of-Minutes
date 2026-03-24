
-- =============================================
-- STORED PROCEDURES FOR MEETING VENUE
-- TABLE : [dbo].[MOM_MeetingVenue]
-- =============================================

IF COL_LENGTH('dbo.MOM_MeetingVenue', 'CompanyName') IS NULL
BEGIN
    ALTER TABLE [dbo].[MOM_MeetingVenue]
    ADD [CompanyName] NVARCHAR(100) NULL;
END;
GO

UPDATE mv
SET mv.CompanyName = seed.CompanyName
FROM [dbo].[MOM_MeetingVenue] mv
CROSS APPLY
(
    SELECT TOP 1 [CompanyName]
    FROM [dbo].[MST_User]
    WHERE [UserRole] = 'Admin'
      AND [CompanyName] IS NOT NULL
    ORDER BY [UserID]
) seed
WHERE mv.CompanyName IS NULL;
GO

-- 1 -> SelectAll Procedure [For List Page]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingVenue_SelectAll]
    @CompanyName NVARCHAR(100)
AS
BEGIN
    SELECT
        [dbo].[MOM_MeetingVenue].[MeetingVenueID],
        [dbo].[MOM_MeetingVenue].[MeetingVenueName],
        [dbo].[MOM_MeetingVenue].[Created],
        [dbo].[MOM_MeetingVenue].[Modified]
    FROM [dbo].[MOM_MeetingVenue]
    WHERE [dbo].[MOM_MeetingVenue].[CompanyName] = @CompanyName
    ORDER BY [dbo].[MOM_MeetingVenue].[MeetingVenueID] DESC;
END;
GO

-- 2 -> SelectByPK Procedure [Edit time record fetch & fill controls]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingVenue_SelectByPK]
(
    @MeetingVenueID INT,
    @CompanyName NVARCHAR(100)
)
AS
BEGIN
    SELECT
        [dbo].[MOM_MeetingVenue].[MeetingVenueID],
        [dbo].[MOM_MeetingVenue].[MeetingVenueName],
        [dbo].[MOM_MeetingVenue].[Created],
        [dbo].[MOM_MeetingVenue].[Modified]
    FROM [dbo].[MOM_MeetingVenue]
    WHERE [dbo].[MOM_MeetingVenue].[MeetingVenueID] = @MeetingVenueID
      AND [dbo].[MOM_MeetingVenue].[CompanyName] = @CompanyName;
END;
GO

-- 3 -> Insert Procedure [To add any new record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingVenue_Insert]
(
    @MeetingVenueName NVARCHAR(100),
    @CompanyName      NVARCHAR(100),
    @Modified         DATETIME
)
AS
BEGIN
    INSERT INTO [dbo].[MOM_MeetingVenue]
    (
        [MeetingVenueName],
        [CompanyName],
        [Created],
        [Modified]
    )
    VALUES
    (
        @MeetingVenueName,
        @CompanyName,
        GETDATE(),
        @Modified
    );
END;
GO

-- 4 -> UpdateByPK Procedure [To update/modify existing record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingVenue_UpdateByPK]
(
    @MeetingVenueID   INT,
    @MeetingVenueName NVARCHAR(100),
    @CompanyName      NVARCHAR(100)
)
AS
BEGIN
    UPDATE [dbo].[MOM_MeetingVenue]
    SET
        [MeetingVenueName] = @MeetingVenueName,
        [Modified]         = GETDATE()
    WHERE [MeetingVenueID] = @MeetingVenueID
      AND [CompanyName] = @CompanyName;
END;
GO

-- 5 -> DeleteByPK Procedure [To delete record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingVenue_DeleteByPK]
(
    @MeetingVenueID INT
)
AS
BEGIN
    DELETE
    FROM [dbo].[MOM_MeetingVenue]
    WHERE [dbo].[MOM_MeetingVenue].[MeetingVenueID] = @MeetingVenueID;
END;
GO
