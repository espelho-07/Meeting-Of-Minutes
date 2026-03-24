-- =============================================
-- STORED PROCEDURES FOR DEPARTMENT
-- TABLE : [dbo].[MOM_Department]
-- =============================================

IF COL_LENGTH('dbo.MOM_Department', 'CompanyName') IS NULL
BEGIN
    ALTER TABLE [dbo].[MOM_Department] ADD [CompanyName] NVARCHAR(100) NULL;
END
GO

UPDATE dbo.MOM_Department
SET CompanyName = ISNULL(NULLIF(CompanyName, ''), 'Default Company')
WHERE CompanyName IS NULL OR CompanyName = '';
GO

-- 1 -> SelectAll Procedure [For List Page]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Department_SelectAll]
(
    @CompanyName NVARCHAR(100),
    @searchtext NVARCHAR(100) = NULL
)
AS
BEGIN
    SELECT
        d.DepartmentID,
        d.DepartmentName,
        d.CompanyName,
        ISNULL(s.StaffCount,0) AS StaffCount,
        ISNULL(m.MeetingsCount,0) AS MeetingsCount
    FROM MOM_Department d
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS StaffCount FROM MOM_Staff GROUP BY DepartmentID) s ON d.DepartmentID = s.DepartmentID
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS MeetingsCount FROM MOM_Meetings GROUP BY DepartmentID) m ON d.DepartmentID = m.DepartmentID
    WHERE d.CompanyName = @CompanyName
      AND (@searchtext IS NULL OR d.DepartmentName LIKE '%' + @searchtext + '%')
    ORDER BY d.DepartmentID DESC;
END;
GO

-- 2 -> SelectByPK Procedure [Edit time record fetch & fill controls]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Department_SelectByPK]
(
    @DepartmentID INT,
    @CompanyName NVARCHAR(100)
)
AS
BEGIN
    SELECT
        d.DepartmentID,
        d.DepartmentName,
        d.CompanyName,
        ISNULL(s.StaffCount,0) AS StaffCount,
        ISNULL(m.MeetingsCount,0) AS MeetingsCount
    FROM MOM_Department d
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS StaffCount FROM MOM_Staff GROUP BY DepartmentID) s ON d.DepartmentID = s.DepartmentID
    LEFT JOIN (SELECT DepartmentID, COUNT(*) AS MeetingsCount FROM MOM_Meetings GROUP BY DepartmentID) m ON d.DepartmentID = m.DepartmentID
    WHERE d.DepartmentID = @DepartmentID
      AND d.CompanyName = @CompanyName;
END;
GO

-- 3 -> Insert Procedure [To add any new record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Department_Insert]
(
    @DepartmentName NVARCHAR(100),
    @CompanyName NVARCHAR(100),
    @Modified DATETIME
)
AS
BEGIN
    INSERT INTO [dbo].[MOM_Department]
    (
        [DepartmentName],
        [CompanyName],
        [Created],
        [Modified]
    )
    VALUES
    (
        @DepartmentName,
        @CompanyName,
        GETDATE(),
        @Modified
    );
END;
GO

-- 4 -> UpdateByPK Procedure [To update/modify existing record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Department_UpdateByPK]
(
    @DepartmentID INT,
    @DepartmentName NVARCHAR(100),
    @CompanyName NVARCHAR(100)
)
AS
BEGIN
    UPDATE [dbo].[MOM_Department]
    SET
        [DepartmentName] = @DepartmentName,
        [CompanyName] = @CompanyName,
        [Modified] = GETDATE()
    WHERE [DepartmentID] = @DepartmentID
      AND [CompanyName] = @CompanyName;
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
