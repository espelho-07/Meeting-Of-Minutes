USE [MOM];
GO

IF OBJECT_ID('dbo.MST_Role', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MST_Role
    (
        RoleID INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(30) NOT NULL UNIQUE,
        DisplayName NVARCHAR(60) NOT NULL,
        Description NVARCHAR(250) NULL,
        IsSystemRole BIT NOT NULL CONSTRAINT DF_MST_Role_IsSystemRole DEFAULT(1),
        Created DATETIME NOT NULL CONSTRAINT DF_MST_Role_Created DEFAULT(GETDATE())
    );
END
GO

IF OBJECT_ID('dbo.MST_Permission', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MST_Permission
    (
        PermissionID INT IDENTITY(1,1) PRIMARY KEY,
        PermissionKey NVARCHAR(80) NOT NULL UNIQUE,
        ModuleName NVARCHAR(60) NOT NULL,
        Description NVARCHAR(250) NOT NULL,
        Created DATETIME NOT NULL CONSTRAINT DF_MST_Permission_Created DEFAULT(GETDATE())
    );
END
GO

IF OBJECT_ID('dbo.MST_RolePermission', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MST_RolePermission
    (
        RolePermissionID INT IDENTITY(1,1) PRIMARY KEY,
        RoleID INT NOT NULL,
        PermissionID INT NOT NULL,
        Created DATETIME NOT NULL CONSTRAINT DF_MST_RolePermission_Created DEFAULT(GETDATE()),
        CONSTRAINT FK_MST_RolePermission_Role FOREIGN KEY (RoleID) REFERENCES dbo.MST_Role(RoleID),
        CONSTRAINT FK_MST_RolePermission_Permission FOREIGN KEY (PermissionID) REFERENCES dbo.MST_Permission(PermissionID),
        CONSTRAINT UQ_MST_RolePermission UNIQUE (RoleID, PermissionID)
    );
END
GO

IF OBJECT_ID('dbo.MST_SystemSetting', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MST_SystemSetting
    (
        SystemSettingID INT IDENTITY(1,1) PRIMARY KEY,
        SettingKey NVARCHAR(80) NOT NULL UNIQUE,
        SettingValue NVARCHAR(MAX) NULL,
        SettingGroup NVARCHAR(50) NOT NULL,
        Description NVARCHAR(250) NULL,
        Modified DATETIME NOT NULL CONSTRAINT DF_MST_SystemSetting_Modified DEFAULT(GETDATE())
    );
END
GO

MERGE dbo.MST_Role AS target
USING (VALUES
    ('SuperAdmin', 'Super Admin', 'Global platform authority with system-wide governance access.'),
    ('Admin', 'Admin', 'Company-scoped operational administrator.'),
    ('User', 'User', 'Standard end user with self-service access.')
) AS source(RoleName, DisplayName, Description)
ON target.RoleName = source.RoleName
WHEN MATCHED THEN
    UPDATE SET DisplayName = source.DisplayName, Description = source.Description
WHEN NOT MATCHED THEN
    INSERT (RoleName, DisplayName, Description, IsSystemRole)
    VALUES (source.RoleName, source.DisplayName, source.Description, 1);
GO

MERGE dbo.MST_Permission AS target
USING (VALUES
    ('dashboard.global', 'Dashboard', 'Global operational dashboard across all platform accounts.'),
    ('dashboard.company', 'Dashboard', 'Company-scoped dashboard and operations visibility.'),
    ('dashboard.self', 'Dashboard', 'Personal meeting dashboard and request view.'),
    ('analytics.global', 'Analytics', 'Cross-company analytics and reporting.'),
    ('analytics.company', 'Analytics', 'Company analytics and performance charts.'),
    ('actioncenter.manage', 'Workflow', 'Manage approval queue and operational actions.'),
    ('actioncenter.self', 'Workflow', 'Review personal action queue.'),
    ('activity.global', 'Audit', 'Read global activity and access-risk logs.'),
    ('activity.company', 'Audit', 'Read company audit trail and risk logs.'),
    ('activity.self', 'Audit', 'Read personal activity history.'),
    ('departments.manage', 'Master Data', 'Create, update, import and remove departments.'),
    ('staff.manage', 'Master Data', 'Manage staff records and transfer workflows.'),
    ('meetingtypes.manage', 'Master Data', 'Manage meeting type catalog.'),
    ('venues.manage', 'Master Data', 'Manage meeting venues.'),
    ('meetings.manage', 'Meetings', 'Schedule meetings, edit, cancel and manage attendees.'),
    ('attendance.manage', 'Meetings', 'Track and update attendance records.'),
    ('profile.self', 'Account', 'Update own profile and password.'),
    ('profile.requests.manage', 'Account', 'Approve or reject profile update requests.'),
    ('transfers.manage', 'Account', 'Approve or reject staff transfer requests.'),
    ('imports.manage', 'Operations', 'Bulk import records using Excel templates.'),
    ('importhistory.view', 'Operations', 'Review import history and generated reports.'),
    ('users.manage.global', 'Access Control', 'Manage all users across every company.'),
    ('users.manage.company', 'Access Control', 'Manage users inside current company only.'),
    ('users.assign.admin', 'Access Control', 'Promote, demote or create admin accounts.'),
    ('users.resetpassword.global', 'Access Control', 'Reset passwords across all users.'),
    ('roles.manage', 'Access Control', 'Review and govern system role matrix.'),
    ('settings.manage', 'Platform', 'Maintain system-wide settings and registration rules.')
) AS source(PermissionKey, ModuleName, Description)
ON target.PermissionKey = source.PermissionKey
WHEN MATCHED THEN
    UPDATE SET ModuleName = source.ModuleName, Description = source.Description
WHEN NOT MATCHED THEN
    INSERT (PermissionKey, ModuleName, Description)
    VALUES (source.PermissionKey, source.ModuleName, source.Description);
GO

DELETE rp
FROM dbo.MST_RolePermission rp
INNER JOIN dbo.MST_Role r ON r.RoleID = rp.RoleID
WHERE r.RoleName IN ('SuperAdmin', 'Admin', 'User');
GO

INSERT INTO dbo.MST_RolePermission (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM dbo.MST_Role r
INNER JOIN dbo.MST_Permission p ON p.PermissionKey IN
(
    'dashboard.global','analytics.global','actioncenter.manage','activity.global',
    'departments.manage','staff.manage','meetingtypes.manage','venues.manage','meetings.manage',
    'attendance.manage','profile.self','profile.requests.manage','transfers.manage',
    'imports.manage','importhistory.view','users.manage.global','users.assign.admin',
    'users.resetpassword.global','roles.manage','settings.manage'
)
WHERE r.RoleName = 'SuperAdmin';
GO

INSERT INTO dbo.MST_RolePermission (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM dbo.MST_Role r
INNER JOIN dbo.MST_Permission p ON p.PermissionKey IN
(
    'dashboard.company','analytics.company','actioncenter.manage','activity.company',
    'departments.manage','staff.manage','meetingtypes.manage','venues.manage','meetings.manage',
    'attendance.manage','profile.self','profile.requests.manage','transfers.manage',
    'imports.manage','importhistory.view','users.manage.company'
)
WHERE r.RoleName = 'Admin';
GO

INSERT INTO dbo.MST_RolePermission (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM dbo.MST_Role r
INNER JOIN dbo.MST_Permission p ON p.PermissionKey IN
(
    'dashboard.self','actioncenter.self','activity.self','profile.self'
)
WHERE r.RoleName = 'User';
GO

MERGE dbo.MST_SystemSetting AS target
USING (VALUES
    ('PlatformName', 'Meeting Of Minutes', 'General', 'Platform title'),
    ('SupportEmail', 'support@meetingofminutes.local', 'General', 'Support contact email'),
    ('MaintenanceBanner', '', 'General', 'Optional maintenance banner'),
    ('AllowAdminSelfRegistration', 'False', 'Security', 'Public admin self-registration flag'),
    ('SessionTimeoutMinutes', '30', 'Security', 'Configured session timeout in minutes')
) AS source(SettingKey, SettingValue, SettingGroup, Description)
ON target.SettingKey = source.SettingKey
WHEN MATCHED THEN
    UPDATE SET SettingValue = source.SettingValue, SettingGroup = source.SettingGroup, Description = source.Description, Modified = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (SettingKey, SettingValue, SettingGroup, Description, Modified)
    VALUES (source.SettingKey, source.SettingValue, source.SettingGroup, source.Description, GETDATE());
GO
