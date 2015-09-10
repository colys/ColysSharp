CREATE TABLE [dbo].A0_Agencys
(
	[Id] INT NOT NULL PRIMARY KEY identity,     
    [LegalPerson] VARCHAR(50) NULL, 
    [Company] VARCHAR(MAX) NULL, 
	[Address] VARCHAR(MAX) NULL, 
    [LoginUser] INT NULL, 
    [CompanyPhone] VARCHAR(50) NULL, 
    [BusinessLicense] VARCHAR(50) NULL, 
    [BusinessLicensePic] VARCHAR(256) NULL, 
    [BankName1] VARCHAR(250) NULL, 
	[BankName2] VARCHAR(250) NULL, 
	[BankName3] VARCHAR(250) NULL, 
    [AccountName] VARCHAR(250) NULL, 
    [BankAccount] VARCHAR(32) NULL, 
    [CreateTime] DATETIME NOT NULL DEFAULT getdate(), 
    [AcceptTime] DATETIME NULL,
	 [Status] INT NOT NULL DEFAULT 0, 
    [AgencyCode] VARCHAR(64) NOT NULL, 
    [ParentCode] VARCHAR(64) NOT NULL
)
