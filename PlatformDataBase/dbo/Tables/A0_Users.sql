CREATE TABLE A0_Users
(
	[UserId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] NVARCHAR(50) NOT NULL, 
	[HeadPicture] VARCHAR(MAX) NULL,
    [Account] NVARCHAR(50) NOT NULL, 
    [WeiXin] NVARCHAR(50) NOT NULL, 
    [Password] VARCHAR(64) NOT NULL, 
	[ZiEBaoPassword] VARCHAR(64) NULL,
    [Mobile] VARCHAR(12) NOT NULL, 
    [Email] VARCHAR(50) NULL, 
    [QQ] VARCHAR(50) NULL, 
    [Birthday] VARCHAR(50) NULL, 
    [IDCard] NCHAR(10) NULL, 
    [CreateTime] DATETIME NULL, 
	[ReferenceUserID] int null,
    [Linkman] VARCHAR(50) NULL, 
    [LinkmanPhone] VARCHAR(20) NULL, 
    [LinkmanRelation] VARCHAR(20) NULL, 
    [EmailOK] TINYINT NOT NULL DEFAULT 0,
	[ZiEBaoBalance] FLOAT NULL,
	[Status] INT NOT NULL DEFAULT 0, 
    [BankName1] VARCHAR(128) NULL, 
    [BankName2] VARCHAR(128) NULL, 
    [BankName3] VARCHAR(128) NULL, 
    [BankAccount] NCHAR(10) NULL
    
)
