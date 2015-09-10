CREATE TABLE [dbo].[A0_Business]
(
	[Id] INT NOT NULL PRIMARY KEY identity, 
    [Name] VARCHAR(20) NULL, 
    [CompanyID] INT NOT NULL DEFAULT 0
)
