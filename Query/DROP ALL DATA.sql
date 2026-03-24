USE [MOM];
GO

DELETE FROM [dbo].[MOM_MeetingMember];
DELETE FROM [dbo].[MOM_Meetings];
DELETE FROM [dbo].[MOM_Staff];
DELETE FROM [dbo].[MOM_MeetingVenue];
DELETE FROM [dbo].[MOM_MeetingType];
DELETE FROM [dbo].[MOM_Department];
DELETE FROM [dbo].[MST_User];
GO

DBCC CHECKIDENT ('[dbo].[MOM_MeetingMember]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[MOM_Meetings]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[MOM_Staff]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[MOM_MeetingVenue]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[MOM_MeetingType]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[MOM_Department]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[MST_User]', RESEED, 0);
GO
