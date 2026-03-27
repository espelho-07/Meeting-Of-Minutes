-- =============================================
-- STORED PROCEDURES FOR STAFF
-- TABLE : [dbo].[MOM_Staff]
-- =============================================

-- 1 -> SelectAll Procedure [For List Page]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Staff_SelectAll]
(
    @searchtext NVARCHAR(100) = NULL
)
AS
BEGIN
    SELECT
        s.[StaffID],
        s.[DepartmentID],
        d.[DepartmentName],
        d.[CompanyName],
        s.[StaffName],
        s.[MobileNo],
        s.[EmailAddress],
        s.[Remarks],
        s.[Created],
        s.[Modified]
    FROM [dbo].[MOM_Staff] s
    INNER JOIN [dbo].[MOM_Department] d
        ON s.[DepartmentID] = d.[DepartmentID]
    WHERE @searchtext IS NULL
       OR s.[StaffName] LIKE '%' + @searchtext + '%'
       OR d.[DepartmentName] LIKE '%' + @searchtext + '%'
       OR s.[EmailAddress] LIKE '%' + @searchtext + '%'
    ORDER BY s.[StaffID] DESC
END;
GO

-- 2 -> SelectByPK Procedure [Edit time record fetch & fill controls]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Staff_SelectByPK]
(
    @StaffID INT
)
AS
BEGIN
    SELECT
        s.[StaffID],
        s.[DepartmentID],
        d.[DepartmentName],
        d.[CompanyName],
        s.[StaffName],
        s.[MobileNo],
        s.[EmailAddress],
        s.[Remarks],
        s.[Created],
        s.[Modified]
    FROM [dbo].[MOM_Staff] s
    INNER JOIN [dbo].[MOM_Department] d
        ON s.[DepartmentID] = d.[DepartmentID]
    WHERE s.[StaffID] = @StaffID;
END;
GO

-- 3 -> Insert Procedure [To add any new record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Staff_Insert]
(
    @DepartmentID  INT,
    @StaffName     NVARCHAR(50),
    @MobileNo      NVARCHAR(20),
    @EmailAddress  NVARCHAR(50),
    @Remarks       NVARCHAR(250),
    @Modified      DATETIME
)
AS
BEGIN
    INSERT INTO [dbo].[MOM_Staff]
    (
        [DepartmentID],
        [StaffName],
        [MobileNo],
        [EmailAddress],
        [Remarks],
        [Created],
        [Modified]
    )
    VALUES
    (
        @DepartmentID,
        @StaffName,
        @MobileNo,
        @EmailAddress,
        @Remarks,
        GETDATE(),
        @Modified
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS StaffID;
END;
GO

GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Staff_SelectByPKForView]
(
    @StaffID INT
)
AS
BEGIN
    SELECT
        s.[StaffID],
        s.[DepartmentID],
        d.[DepartmentName],
        d.[CompanyName],
        s.[StaffName],
        s.[MobileNo],
        s.[EmailAddress],
        s.[Remarks],
        s.[Created],
        s.[Modified],
        ISNULL(u.[UserName], '') AS [LoginUserName],
        CAST('' AS NVARCHAR(50)) AS [LoginPassword],
        ISNULL(u.[IsAutoPassword], 0) AS [IsAutoPassword],
        (
            SELECT COUNT(*)
            FROM dbo.MOM_MeetingMember mm
            WHERE mm.StaffID = s.StaffID
        ) AS [EnrolledMeetingsCount]
    FROM dbo.MOM_Staff s
    INNER JOIN dbo.MOM_Department d
        ON s.DepartmentID = d.DepartmentID
    LEFT JOIN dbo.MST_User u
        ON u.StaffID = s.StaffID
    WHERE s.StaffID = @StaffID;
END;
GO

-- 4 -> UpdateByPK Procedure [To update/modify existing record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Staff_UpdateByPK]
(
    @StaffID      INT,
    @DepartmentID INT,
    @StaffName    NVARCHAR(50),
    @MobileNo     NVARCHAR(20),
    @EmailAddress NVARCHAR(50),
    @Remarks      NVARCHAR(250)
)
AS
BEGIN
    UPDATE [dbo].[MOM_Staff]
    SET
        [DepartmentID] = @DepartmentID,
        [StaffName]    = @StaffName,
        [MobileNo]     = @MobileNo,
        [EmailAddress] = @EmailAddress,
        [Remarks]      = @Remarks,
        [Modified]     = GETDATE()
    WHERE [StaffID] = @StaffID;
END;
GO

-- 5 -> DeleteByPK Procedure [To delete record]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Staff_DeleteByPK]
(
    @StaffID INT
)
AS
BEGIN
    DELETE
    FROM [dbo].[MOM_Staff]
    WHERE [dbo].[MOM_Staff].[StaffID] = @StaffID;
END;
GO

