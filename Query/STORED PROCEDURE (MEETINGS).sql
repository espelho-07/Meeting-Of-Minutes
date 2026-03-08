-- =============================================
-- STORED PROCEDURES FOR MEETINGS
-- TABLE : [dbo].[MOM_Meetings]
-- =============================================

-- 1 -> SelectAll Procedure [For List Page]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Meetings_SelectAll]
AS
BEGIN
    SELECT
        [dbo].[MOM_Meetings].[MeetingID],
        [dbo].[MOM_Meetings].[MeetingDate],
        [dbo].[MOM_MeetingVenue].[MeetingVenueName],
        [dbo].[MOM_MeetingType].[MeetingTypeName],
        [dbo].[MOM_Department].[DepartmentName],
        [dbo].[MOM_Meetings].[MeetingDescription],
        [dbo].[MOM_Meetings].[DocumentPath],
        [dbo].[MOM_Meetings].[IsCancelled],
        [dbo].[MOM_Meetings].[CancellationDateTime],
        [dbo].[MOM_Meetings].[CancellationReason],
        [dbo].[MOM_Meetings].[Created],
        [dbo].[MOM_Meetings].[Modified]
    FROM [dbo].[MOM_Meetings]
    INNER JOIN [dbo].[MOM_MeetingType]
        ON [dbo].[MOM_Meetings].[MeetingTypeID] = [dbo].[MOM_MeetingType].[MeetingTypeID]
    INNER JOIN [dbo].[MOM_MeetingVenue]
        ON [dbo].[MOM_MeetingVenue].[MeetingVenueID] = [dbo].[MOM_Meetings].[MeetingVenueID]
    INNER JOIN [dbo].[MOM_Department]
        ON [dbo].[MOM_Department].[DepartmentID] = [dbo].[MOM_Meetings].[DepartmentID]
    ORDER BY [dbo].[MOM_Meetings].[MeetingID] DESC;
END;
GO

-- 2 -> SelectByPK Procedure [Edit time record fetch & fill controls]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Meetings_SelectByPK]
(
    @MeetingID INT
)
AS
BEGIN
    SELECT
        [dbo].[MOM_Meetings].[MeetingID],
        [dbo].[MOM_Meetings].[MeetingDate],
        [dbo].[MOM_Meetings].[MeetingVenueID],
        [dbo].[MOM_Meetings].[MeetingTypeID],
        [dbo].[MOM_Meetings].[DepartmentID],
        [dbo].[MOM_Meetings].[MeetingDescription],
        [dbo].[MOM_Meetings].[DocumentPath],
        [dbo].[MOM_Meetings].[IsCancelled],
        [dbo].[MOM_Meetings].[CancellationDateTime],
        [dbo].[MOM_Meetings].[CancellationReason],
        [dbo].[MOM_Meetings].[Created],
        [dbo].[MOM_Meetings].[Modified]
    FROM [dbo].[MOM_Meetings]
    INNER JOIN [dbo].[MOM_MeetingType]
        ON [dbo].[MOM_Meetings].[MeetingTypeID] = [dbo].[MOM_MeetingType].[MeetingTypeID]
    INNER JOIN [dbo].[MOM_MeetingVenue]
        ON [dbo].[MOM_MeetingVenue].[MeetingVenueID] = [dbo].[MOM_Meetings].[MeetingVenueID]
    INNER JOIN [dbo].[MOM_Department]
        ON [dbo].[MOM_Department].[DepartmentID] = [dbo].[MOM_Meetings].[DepartmentID]
    WHERE [dbo].[MOM_Meetings].[MeetingID] = @MeetingID;
END;
GO

-- 3 -> Insert Procedure [To add any new record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Meetings_Insert]
(
    @MeetingDate        DATETIME,
    @MeetingVenueID     INT,
    @MeetingTypeID      INT,
    @DepartmentID       INT,
    @MeetingDescription NVARCHAR(250),
    @DocumentPath       NVARCHAR(250),
    @Modified           DATETIME
)
AS
BEGIN
    INSERT INTO [dbo].[MOM_Meetings]
    (
        [MeetingDate],
        [MeetingVenueID],
        [MeetingTypeID],
        [DepartmentID],
        [MeetingDescription],
        [DocumentPath],
        [Created],
        [Modified]
    )
    VALUES
    (
        @MeetingDate,
        @MeetingVenueID,
        @MeetingTypeID,
        @DepartmentID,
        @MeetingDescription,
        @DocumentPath,
        GETDATE(),
        @Modified
    );
END;
GO

-- 4 -> UpdateByPK Procedure [To update/modify existing record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Meetings_UpdateByPK]
(
    @MeetingID          INT,
    @MeetingDate        DATETIME,
    @MeetingVenueID     INT,
    @MeetingTypeID      INT,
    @DepartmentID       INT,
    @MeetingDescription NVARCHAR(250),
    @DocumentPath       NVARCHAR(250)
)
AS
BEGIN
    UPDATE [dbo].[MOM_Meetings]
    SET
        [MeetingDate]        = @MeetingDate,
        [MeetingVenueID]     = @MeetingVenueID,
        [MeetingTypeID]      = @MeetingTypeID,
        [DepartmentID]       = @DepartmentID,
        [MeetingDescription] = @MeetingDescription,
        [DocumentPath]       = @DocumentPath,
        [Modified]           = GETDATE()
    WHERE [MeetingID] = @MeetingID;
END;
GO

-- 5 -> DeleteByPK Procedure [To delete record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Meetings_DeleteByPK]
(
    @MeetingID INT
)
AS
BEGIN
    DELETE
    FROM [dbo].[MOM_Meetings]
    WHERE [dbo].[MOM_Meetings].[MeetingID] = @MeetingID;
END;
GO


INSERT INTO [dbo].[MOM_Meetings]
(
    MeetingDate,
    MeetingTypeID,
    MeetingVenueID,
    DepartmentID,
    MeetingDescription,
    DocumentPath,
    IsCancelled,
    CancellationDateTime,
    CancellationReason,
    Created,
    Modified
)
VALUES
('2026-02-01', 1, 1, 1, 'IT Planning Meeting', '/docs/it.pdf', 0, NULL, NULL, GETDATE(), GETDATE()),

('2026-02-02', 2, 2, 2, 'HR External Discussion', '/docs/hr.pdf', 0, NULL, NULL, GETDATE(), GETDATE()),

('2026-02-03', 3, 3, 3, 'Finance Review Meeting', '/docs/finance.pdf', 1, GETDATE(), 'Budget Issue', GETDATE(), GETDATE()),

('2026-02-04', 4, 4, 4, 'Marketing Strategy Planning', '/docs/marketing.pdf', 0, NULL, NULL, GETDATE(), GETDATE()),

('2026-02-05', 5, 5, 5, 'Operations Emergency Meeting', '/docs/ops.pdf', 1, GETDATE(), 'Urgent Issue', GETDATE(), GETDATE());


-- DROPDOWN LISTS
go
CREATE OR ALTER PROC PR_MOM_DEPARTMENT_DDL
AS
BEGIN
	SELECT DepartmentID , DepartmentName
	FROM MOM_Department
	ORDER BY DepartmentName
END
GO
CREATE OR ALTER PROC PR_MOM_MEETINGVENUE_DDL
AS
BEGIN
	SELECT MeetingVenueID , MeetingVenueName
	FROM MOM_MeetingVenue
	ORDER BY MeetingVenueName
END
GO
CREATE OR ALTER PROC PR_MOM_MEETINGTYPE_DDL
AS
BEGIN
	SELECT MeetingTypeID , MeetingTypeName
	FROM MOM_MeetingType
	ORDER BY MeetingTypeName
END
GO