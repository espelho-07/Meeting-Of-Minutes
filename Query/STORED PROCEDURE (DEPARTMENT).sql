-- =============================================
-- STORED PROCEDURES FOR DEPARTMENT
-- TABLE : [dbo].[MOM_Department]
-- =============================================

-- 1 -> SelectAll Procedure [For List Page]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Department_SelectAll]
AS
BEGIN
    SELECT
        d.DepartmentID,
        d.DepartmentName,
        ISNULL(s.StaffCount,0) AS StaffCount,
        ISNULL(m.MeetingsCount,0) AS MeetingsCount
    FROM MOM_Department d
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS StaffCount FROM MOM_Staff GROUP BY DepartmentID) s ON d.DepartmentID = s.DepartmentID
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS MeetingsCount FROM MOM_Meetings GROUP BY DepartmentID) m ON d.DepartmentID = m.DepartmentID
    ORDER BY d.DepartmentID DESC;
END;
GO

-- 2 -> SelectByPK Procedure [Edit time record fetch & fill controls]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Department_SelectByPK]
(
    @DepartmentID INT
)
AS
BEGIN
    SELECT
        d.DepartmentID,
        d.DepartmentName,
        ISNULL(s.StaffCount,0) AS StaffCount,
        ISNULL(m.MeetingsCount,0) AS MeetingsCount
    FROM MOM_Department d
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS StaffCount FROM MOM_Staff GROUP BY DepartmentID) s ON d.DepartmentID = s.DepartmentID
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS MeetingsCount FROM MOM_Meetings GROUP BY DepartmentID) m ON d.DepartmentID = m.DepartmentID
    WHERE d.DepartmentID = @DepartmentID;
END;
GO

-- 3 -> Insert Procedure [To add any new record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Department_Insert]
(
    @DepartmentName NVARCHAR(100),
    @Modified       DATETIME
)
AS
BEGIN
    INSERT INTO [dbo].[MOM_Department]
    (
        [DepartmentName],
        [Created],
        [Modified]
    )
    VALUES
    (
        @DepartmentName,
        GETDATE(),
        @Modified
    );
END;
GO

-- 4 -> UpdateByPK Procedure [To update/modify existing record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Department_UpdateByPK]
(
    @DepartmentID   INT,
    @DepartmentName NVARCHAR(100)
)
AS
BEGIN
    UPDATE [dbo].[MOM_Department]
    SET
        [DepartmentName] = @DepartmentName,
        [Modified]       = GETDATE()
    WHERE [DepartmentID] = @DepartmentID;
END;
GO

-- 5 -> DeleteByPK Procedure [To delete record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Department_DeleteByPK]
(
    @DepartmentID INT
)
AS
BEGIN
    DELETE
    FROM [dbo].[MOM_Department]
    WHERE [dbo].[MOM_Department].[DepartmentID] = @DepartmentID;
END;
GO



INSERT INTO [dbo].[MOM_Department]
(
    DepartmentName,
    Created,
    Modified
)
VALUES
('Human Resources',     GETDATE(), GETDATE()),
('Information Technology', GETDATE(), GETDATE()),
('Finance',             GETDATE(), GETDATE()),
('Marketing',           GETDATE(), GETDATE()),
('Customer Support',    GETDATE(), GETDATE());

-- 6 -> SelectAll With Counts [Department + StaffCount + MeetingsCount]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Department_SelectAllWithCounts]
AS
BEGIN
    SELECT
        d.DepartmentID,
        d.DepartmentName,
        ISNULL(s.StaffCount,0) AS StaffCount,
        ISNULL(m.MeetingsCount,0) AS MeetingsCount
    FROM MOM_Department d
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS StaffCount FROM MOM_Staff GROUP BY DepartmentID) s ON d.DepartmentID = s.DepartmentID
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS MeetingsCount FROM MOM_Meetings GROUP BY DepartmentID) m ON d.DepartmentID = m.DepartmentID
    ORDER BY d.DepartmentID DESC;
END;
GO

-- 7 -> SelectByPK With Counts [Department + StaffCount + MeetingsCount]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Department_SelectByPKWithCounts]
(
    @DepartmentID INT
)
AS
BEGIN
    SELECT
        d.DepartmentID,
        d.DepartmentName,
        ISNULL(s.StaffCount,0) AS StaffCount,
        ISNULL(m.MeetingsCount,0) AS MeetingsCount
    FROM MOM_Department d
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS StaffCount FROM MOM_Staff GROUP BY DepartmentID) s ON d.DepartmentID = s.DepartmentID
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS MeetingsCount FROM MOM_Meetings GROUP BY DepartmentID) m ON d.DepartmentID = m.DepartmentID
    WHERE d.DepartmentID = @DepartmentID;
END;
GO


