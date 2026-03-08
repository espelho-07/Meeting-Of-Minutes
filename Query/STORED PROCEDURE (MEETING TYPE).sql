-- =============================================
-- STORED PROCEDURES FOR MEETING TYPE
-- TABLE : [dbo].[MOM_MeetingType]
-- =============================================

-- 1 -> SelectAll Procedure [For List Page]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingType_SelectAll]
AS
BEGIN
    SELECT
        [dbo].[MOM_MeetingType].[MeetingTypeID],
        [dbo].[MOM_MeetingType].[MeetingTypeName],
        [dbo].[MOM_MeetingType].[Remarks],
        [dbo].[MOM_MeetingType].[Created],
        [dbo].[MOM_MeetingType].[Modified]
    FROM [dbo].[MOM_MeetingType]
    ORDER BY [dbo].[MOM_MeetingType].[MeetingTypeID] DESC;
END;
GO

-- 2 -> SelectByPK Procedure [Edit time record fetch & fill controls]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingType_SelectByPK]
(
    @MeetingTypeID INT
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
    WHERE [dbo].[MOM_MeetingType].[MeetingTypeID] = @MeetingTypeID;
END;
GO

-- 3 -> Insert Procedure [To add any new record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingType_Insert]
(
    @MeetingTypeName NVARCHAR(100),
    @Remarks         NVARCHAR(100),
    @Modified        DATETIME
)
AS
BEGIN
    INSERT INTO [dbo].[MOM_MeetingType]
    (
        [MeetingTypeName],
        [Remarks],
        [Created],
        [Modified]
    )
    VALUES
    (
        @MeetingTypeName,
        @Remarks,
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
    @Remarks         NVARCHAR(100)
)
AS
BEGIN
    UPDATE [dbo].[MOM_MeetingType]
    SET
        [MeetingTypeName] = @MeetingTypeName,
        [Remarks]         = @Remarks,
        [Modified]        = GETDATE()
    WHERE [MeetingTypeID] = @MeetingTypeID;
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
    Created,
    Modified
)
VALUES
('Internal', 'Internal department meeting', GETDATE(), GETDATE()),
('External', 'Meeting with external clients', GETDATE(), GETDATE()),
('Review', 'Quarterly review meeting', GETDATE(), GETDATE()),
('Planning', 'Strategic planning discussion', GETDATE(), GETDATE()),
('Emergency', 'Urgent issue handling meeting', GETDATE(), GETDATE());
