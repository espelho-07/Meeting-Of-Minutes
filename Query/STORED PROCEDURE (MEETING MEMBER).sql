
-- =============================================
-- STORED PROCEDURES FOR MEETING MEMBERS
-- TABLE : [dbo].[MOM_MeetingMember]
-- =============================================

-- 1 -> SelectAll Procedure [For List Page]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingMember_SelectAll]
AS
BEGIN
    SELECT
        [dbo].[MOM_MeetingMember].[MeetingMemberID],
        [dbo].[MOM_MeetingMember].[MeetingID],
        [dbo].[MOM_MeetingMember].[StaffID],
        [dbo].[MOM_MeetingMember].[IsPresent],
        [dbo].[MOM_MeetingMember].[Remarks],
        [dbo].[MOM_MeetingMember].[Created],
        [dbo].[MOM_MeetingMember].[Modified]
    FROM [dbo].[MOM_MeetingMember]
    ORDER BY [dbo].[MOM_MeetingMember].[MeetingMemberID] DESC;
END;
GO


-- 2 -> SelectByPK Procedure  [Edit time record fetch & fill controls]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingMember_SelectByPK]
(
    @MeetingMemberID INT
)
AS
BEGIN
    SELECT
        [dbo].[MOM_MeetingMember].[MeetingMemberID],
        [dbo].[MOM_MeetingMember].[MeetingID],
        [dbo].[MOM_MeetingMember].[StaffID],
        [dbo].[MOM_MeetingMember].[IsPresent],
        [dbo].[MOM_MeetingMember].[Remarks],
        [dbo].[MOM_MeetingMember].[Created],
        [dbo].[MOM_MeetingMember].[Modified]
    FROM [dbo].[MOM_MeetingMember]
    WHERE [dbo].[MOM_MeetingMember].[MeetingMemberID] = @MeetingMemberID;
END;
GO

-- 3 -> Insert Procedure [To add meeting member attendance]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingMember_Insert]
(
    @MeetingID INT,
    @StaffID   INT,
    @IsPresent BIT,
    @Remarks   NVARCHAR(250),
    @Modified  DATETIME
)
AS
BEGIN
    INSERT INTO [dbo].[MOM_MeetingMember]
    (
        [MeetingID],
        [StaffID],
        [IsPresent],
        [Remarks],
        [Created],
        [Modified]
    )
    VALUES
    (
        @MeetingID,
        @StaffID,
        @IsPresent,
        @Remarks,
        GETDATE(),
        @Modified
    );
END;
GO

-- 4 -> UpdateByPK Procedure [To update attendance]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingMember_UpdateByPK]
(
    @MeetingMemberID INT,
    @IsPresent       BIT,
    @Remarks         NVARCHAR(250)
)
AS
BEGIN
    UPDATE [dbo].[MOM_MeetingMember]
    SET
        [IsPresent] = @IsPresent,
        [Remarks]   = @Remarks,
        [Modified]  = GETDATE()
    WHERE [MeetingMemberID] = @MeetingMemberID;
END;
GO

-- 5 -> DeleteByPK Procedure [To delete record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_MeetingMember_DeleteByPK]
(
    @MeetingMemberID INT
)
AS
BEGIN
    DELETE
    FROM [dbo].[MOM_MeetingMember]
    WHERE [dbo].[MOM_MeetingMember].[MeetingMemberID] = @MeetingMemberID;
END;
GO


INSERT INTO [dbo].[MOM_MeetingMember]
(
    MeetingID,
    StaffID,
    IsPresent,
    Remarks,
    Created,
    Modified
)
VALUES
(1, 1, 1, 'Attended on time', GETDATE(), GETDATE()),
(2, 2, 1, 'Participated actively', GETDATE(), GETDATE()),
(3, 3, 0, 'Absent due to leave', GETDATE(), GETDATE()),
(4, 4, 1, 'Joined via Zoom', GETDATE(), GETDATE()),
(5, 5, 0, 'Medical emergency', GETDATE(), GETDATE());



