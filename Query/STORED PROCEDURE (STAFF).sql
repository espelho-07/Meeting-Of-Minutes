-- =============================================
-- STORED PROCEDURES FOR STAFF
-- TABLE : [dbo].[MOM_Staff]
-- =============================================

-- 1 -> SelectAll Procedure [For List Page]
GO
CREATE OR ALTER PROCEDURE [dbo].[PR_Staff_SelectAll]
AS
BEGIN
    SELECT
        [dbo].[MOM_Staff].[StaffID],
        [dbo].[MOM_Staff].[DepartmentID],
        [dbo].[MOM_Staff].[StaffName],
        [dbo].[MOM_Staff].[MobileNo],
        [dbo].[MOM_Staff].[EmailAddress],
        [dbo].[MOM_Staff].[Remarks],
        [dbo].[MOM_Staff].[Created],
        [dbo].[MOM_Staff].[Modified]
    FROM [dbo].[MOM_Staff]
    ORDER BY [dbo].[MOM_Staff].[StaffID] DESC
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
        [dbo].[MOM_Staff].[StaffID],
        [dbo].[MOM_Staff].[DepartmentID],
        [dbo].[MOM_Staff].[StaffName],
        [dbo].[MOM_Staff].[MobileNo],
        [dbo].[MOM_Staff].[EmailAddress],
        [dbo].[MOM_Staff].[Remarks],
        [dbo].[MOM_Staff].[Created],
        [dbo].[MOM_Staff].[Modified]
    FROM [dbo].[MOM_Staff]
    WHERE [dbo].[MOM_Staff].[StaffID] = @StaffID;
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

INSERT INTO [dbo].[MOM_Staff]
(
    DepartmentID,
    StaffName,
    MobileNo,
    EmailAddress,
    Remarks,
    Created,
    Modified
)
VALUES
(1, 'Rahul Sharma', '9876543210', 'rahul.sharma@gmail.com', 'HR Staff', GETDATE(), GETDATE()),
(2, 'Priya Patel', '9123456780', 'priya.patel@gmail.com', 'Finance Staff', GETDATE(), GETDATE()),
(3, 'Amit Verma', '9988776655', 'amit.verma@gmail.com', 'IT Support', GETDATE(), GETDATE()),
(1, 'Neha Singh', '9090909090', 'neha.singh@gmail.com', 'Recruiter', GETDATE(), GETDATE()),
(4, 'Suresh Mehta', '9012345678', 'suresh.mehta@gmail.com', 'Admin Staff', GETDATE(), GETDATE());

