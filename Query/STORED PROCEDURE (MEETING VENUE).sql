
-- =============================================
-- STORED PROCEDURES FOR MEETING VENUE
-- TABLE : [dbo].[MOM_MeetingVenue]
-- =============================================

-- 1 -> SelectAll Procedure [For List Page]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingVenue_SelectAll]
AS
BEGIN
    SELECT
        [dbo].[MOM_MeetingVenue].[MeetingVenueID],
        [dbo].[MOM_MeetingVenue].[MeetingVenueName],
        [dbo].[MOM_MeetingVenue].[Created],
        [dbo].[MOM_MeetingVenue].[Modified]
    FROM [dbo].[MOM_MeetingVenue]
    ORDER BY [dbo].[MOM_MeetingVenue].[MeetingVenueID] DESC;
END;
GO

-- 2 -> SelectByPK Procedure [Edit time record fetch & fill controls]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingVenue_SelectByPK]
(
    @MeetingVenueID INT
)
AS
BEGIN
    SELECT
        [dbo].[MOM_MeetingVenue].[MeetingVenueID],
        [dbo].[MOM_MeetingVenue].[MeetingVenueName],
        [dbo].[MOM_MeetingVenue].[Created],
        [dbo].[MOM_MeetingVenue].[Modified]
    FROM [dbo].[MOM_MeetingVenue]
    WHERE [dbo].[MOM_MeetingVenue].[MeetingVenueID] = @MeetingVenueID;
END;
GO

-- 3 -> Insert Procedure [To add any new record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingVenue_Insert]
(
    @MeetingVenueName NVARCHAR(100),
    @Modified         DATETIME
)
AS
BEGIN
    INSERT INTO [dbo].[MOM_MeetingVenue]
    (
        [MeetingVenueName],
        [Created],
        [Modified]
    )
    VALUES
    (
        @MeetingVenueName,
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
    @MeetingVenueName NVARCHAR(100)
)
AS
BEGIN
    UPDATE [dbo].[MOM_MeetingVenue]
    SET
        [MeetingVenueName] = @MeetingVenueName,
        [Modified]         = GETDATE()
    WHERE [MeetingVenueID] = @MeetingVenueID;
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

INSERT INTO [dbo].[MOM_MeetingVenue]
(
    MeetingVenueName,
    Created,
    Modified
)
VALUES
('Conference Room A', GETDATE(), GETDATE()),
('Conference Room B', GETDATE(), GETDATE()),
('Main Hall', GETDATE(), GETDATE()),
('Zoom Meeting', GETDATE(), GETDATE()),
('Board Room', GETDATE(), GETDATE());