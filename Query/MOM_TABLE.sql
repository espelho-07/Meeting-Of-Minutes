-- ============================================
-- DATABASE: MOM (Minutes of Meeting)
-- Description: Complete database schema for 
--              Meeting Management System
-- ============================================

-- Create Database

CREATE DATABASE MOM;
GO
USE MOM;
GO

-- ============================================
-- TABLE 1: MOM_MeetingType
-- Description: Stores different types of meetings
--              (e.g., Daily Standup, Review, etc.)
-- ============================================
CREATE TABLE MOM_MeetingType 
(
    MeetingTypeID       INT PRIMARY KEY IDENTITY(1,1),  -- Auto-increment primary key
    MeetingTypeName     NVARCHAR(100) NOT NULL,         -- Name of meeting type
    Remarks             NVARCHAR(100) NOT NULL,         -- Additional remarks
    Created             DATETIME DEFAULT GETDATE(),     -- Record creation timestamp
    Modified            DATETIME NOT NULL               -- Last modification timestamp
);
GO


-- ============================================
-- TABLE 2: MOM_Department
-- Description: Stores department information
--              (e.g., IT, HR, Finance, etc.)
-- ============================================
CREATE TABLE MOM_Department 
(
    DepartmentID        INT PRIMARY KEY IDENTITY(1,1),  -- Auto-increment primary key
    DepartmentName      NVARCHAR(100) NOT NULL,         -- Name of department
    Created             DATETIME DEFAULT GETDATE(),     -- Record creation timestamp
    Modified            DATETIME NOT NULL               -- Last modification timestamp
);
GO


-- ============================================
-- TABLE 3: MOM_MeetingVenue
-- Description: Stores meeting venue/location details
--              (e.g., Conference Room A, Zoom, etc.)
-- ============================================
CREATE TABLE MOM_MeetingVenue
(
    MeetingVenueID      INT PRIMARY KEY IDENTITY(1,1),  -- Auto-increment primary key
    MeetingVenueName    NVARCHAR(100) NOT NULL,         -- Name of venue
    Created             DATETIME DEFAULT GETDATE(),     -- Record creation timestamp
    Modified            DATETIME NOT NULL               -- Last modification timestamp
);
GO


-- ============================================
-- TABLE 4: MOM_Staff
-- Description: Stores staff/employee information
--              who can attend meetings
-- ============================================
CREATE TABLE MOM_Staff
(
    StaffID             INT PRIMARY KEY IDENTITY(1,1),  -- Auto-increment primary key
    DepartmentID        INT NOT NULL,                   -- FK: Reference to department
    StaffName           NVARCHAR(50) NOT NULL,          -- Full name of staff
    MobileNo            NVARCHAR(20) NOT NULL,          -- Contact number
    EmailAddress        NVARCHAR(50) NOT NULL,          -- Email address
    Remarks             NVARCHAR(250) NULL,             -- Optional remarks
    Created             DATETIME DEFAULT GETDATE(),     -- Record creation timestamp
    Modified            DATETIME NOT NULL,              -- Last modification timestamp

    -- Foreign Key Constraint
    CONSTRAINT FK_Staff_Department 
        FOREIGN KEY (DepartmentID) 
        REFERENCES MOM_Department(DepartmentID)
);
GO


-- ============================================
-- TABLE 5: MOM_Meetings
-- Description: Main table storing all meeting 
--              details including date, venue,
--              type, and cancellation info
-- ============================================
CREATE TABLE MOM_Meetings 
(
    MeetingID               INT PRIMARY KEY IDENTITY(1,1),  -- Auto-increment primary key
    MeetingDate             DATETIME NOT NULL,              -- Date & time of meeting
    MeetingVenueID          INT NOT NULL,                   -- FK: Reference to venue
    MeetingTypeID           INT NOT NULL,                   -- FK: Reference to meeting type
    DepartmentID            INT NOT NULL,                   -- FK: Reference to department
    MeetingDescription      NVARCHAR(250) NULL,             -- Optional meeting description
    DocumentPath            NVARCHAR(250) NULL,             -- Path to attached documents
    Created                 DATETIME DEFAULT GETDATE(),     -- Record creation timestamp
    Modified                DATETIME NOT NULL,              -- Last modification timestamp
    IsCancelled             BIT NULL,                       -- Flag: Is meeting cancelled?
    CancellationDateTime    DATETIME NULL,                  -- When was it cancelled?
    CancellationReason      NVARCHAR(250) NULL,             -- Reason for cancellation

    -- Foreign Key Constraints
    CONSTRAINT FK_Meetings_Venue 
        FOREIGN KEY (MeetingVenueID) 
        REFERENCES MOM_MeetingVenue(MeetingVenueID),
    
    CONSTRAINT FK_Meetings_Type 
        FOREIGN KEY (MeetingTypeID) 
        REFERENCES MOM_MeetingType(MeetingTypeID),
    
    CONSTRAINT FK_Meetings_Department 
        FOREIGN KEY (DepartmentID) 
        REFERENCES MOM_Department(DepartmentID)
);
GO


-- ============================================
-- TABLE 6: MOM_MeetingMember
-- Description: Junction/Bridge table linking
--              meetings with staff members
--              Also tracks attendance
-- ============================================
CREATE TABLE MOM_MeetingMember
(
    MeetingMemberID     INT PRIMARY KEY IDENTITY(1,1),  -- Auto-increment primary key
    MeetingID           INT NOT NULL,                   -- FK: Reference to meeting
    StaffID             INT NOT NULL,                   -- FK: Reference to staff
    IsPresent           BIT NOT NULL,                   -- Flag: Was member present?
    Remarks             NVARCHAR(250) NULL,             -- Optional remarks
    Created             DATETIME DEFAULT GETDATE(),     -- Record creation timestamp
    Modified            DATETIME NOT NULL,              -- Last modification timestamp

    -- Foreign Key Constraints
    CONSTRAINT FK_MeetingMember_Meeting 
        FOREIGN KEY (MeetingID) 
        REFERENCES MOM_Meetings(MeetingID),
    
    CONSTRAINT FK_MeetingMember_Staff 
        FOREIGN KEY (StaffID) 
        REFERENCES MOM_Staff(StaffID)
);
GO


-- ============================================
-- END OF SCRIPT
-- ============================================

/*
    TABLE RELATIONSHIPS SUMMARY:
    ============================
    
    MOM_Department (1) ──────► (N) MOM_Staff
    MOM_Department (1) ──────► (N) MOM_Meetings
    MOM_MeetingType (1) ─────► (N) MOM_Meetings
    MOM_MeetingVenue (1) ────► (N) MOM_Meetings
    MOM_Meetings (1) ────────► (N) MOM_MeetingMember
    MOM_Staff (1) ───────────► (N) MOM_MeetingMember
    
*/